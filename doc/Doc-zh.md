
# Corm 前言
一个 C# 简易 orm 框架, 支持 SqlServer

不支持自动维护数据库表结构，使用的时候，一般是先设计好数据库，之后依据数据库的表结构创建 Entity 类

Github地址为 : [github.com/Ericwyn/Corm](github.com/Ericwyn/Corm)

# Attribute
 - `[CormColumn(Name, Length, SqlDbType)]`
     - 在 Entity 类的属性当中标记数据库的列
 - `[CormTable(TableName)]`
	 - 在 Entity 类上标记数据库的名称

# 使用说明
## 快速开始
### Entity 类创建
    
    [CormTable(TableName = "Student")]
    public class Student
    {
        [CormColumn(Name = "studentName_", Size = 10,DbType = SqlDbType.VarChar)]
        public string studentName { get; set; }
        [CormColumn(Name = "studentAge_", Size = 2, DbType = SqlDbType.Int)]
        public int? studentAge { get; set; }
    }

 - 需要使用可空类型，例如 int?、double?、bool? ，否则 Where 查询会有错误

### 使用 Corm 完成数据库操作

    public static void Main(string[] args)
        {
            var corm = new Corm("server=127.0.0.1;database=corm;uid=TestAccount;pwd=TestAccount");
            var studentTable = new CormTable<Student>(corm);
            List<Student> studentTable.Find().All().Commit();
        }

 - Corm 创建的时候需要传入 SqlConnection String 
 - CormTable 创建的时候可以手动设置对应的表的名字
 - 也可使用 `[CormTable(TableName="xxx")]` 来标记 Entity 类对应的表的名称，无需在创建时候传入

## 具体说明
### Select 查询操作
 - Find All
    
        List<Student> list = studentTable.Find().All().Commit();
 
 - 带 Where 的查询
        
        // 使用一个对象存储 Where 条件
        var selectTemp = new Student();
        // 可使用多个条件来限定
        // selectTemp.studentAge = 10;
        selectTemp.studentName = "aaa";
        List<Student> list = studentTable.Find().Where(selectTemp).Commit();
 
 - 设定需要查询的字段
        
        List<Student> list = studentTable.Find().Attributes(new[] {"studentName_"}).Commit();

 - 手写 Sql 语句进行查询
 
    Corm 支持传入手写的 Sql 语句进行查询，也会自动解析成 Entity 类的 List，并且也提供了事务的支持，可使用以下两个方法
        
    - `Customize(string sqlStr)`
        - 可传入拼接的 Sql
    - `Customize(string sqlStr, SqlParameter[] parameters)`
        - 传入预编译语句以及需要替换的参数
    
    示例代码如下
    
        // SELECT 自定义查询语句
        var list = studentTable.Find().Customize(
            "SELECT * FROM Student WHERE studentName_=@studentName_",
            new SqlParameter[]
            {
                new SqlParameter("@studentName_", "test3"),
            }
        ).Commit();     // 如需要使用事务的话可在此处传入 SqlTransaction 对象
        Console.WriteLine(list);

### Insert 操作
Insert 方法使用 Value 传入需要插入的值，可为一个 Entity 的 List 或者一个单独的 Entity 对象

Insert 是一个事务操作，当插入失败时候，整个插入操作将会回滚


 - Insert 一条数据
        
        var insert1 = new Student
        {
            studentAge = 1, 
            studentName = "inset1",
        };
        studentTable.Insert().Value(insert1).Commit();
 
 - Insert 多条数据
        
        var insert1 = new Student
        {
            studentAge = 1, 
            studentName = "inset1",
        };
        var insert2 = new Student()
        {
            studentAge = 02,
            studentName = "inset2",
        };
        var list = new List<Student>(){
            insert1,
            insert2,
        };
        studentTable.Insert().Value(list).Commit();
        
### Update 操作

Update 使用 Where() 方法设置需要 Update 的条件，使用 Value() 方法设置更新之后的值

下面的代码展示如何将所有 studentName 为 “testtest” 的行，更新他们的 studentAge 为 20

    studentTable.Update().Where(new Student()
    {
        studentName = "testtest",
    })
    .Value(new Student()
    {
        studentAge = 20,
    })
    .Commit();

### Delete 操作

Delete 操作可选择删除表中全部数据，或按照特定条件删除

 - 删除全部数据
 
        studentTable.Delete().All().Commit();
 
 - 按照特定条件删除
 
        // 删除所有 studentName 为 "testtest" , studentAge 为 20 的行
        studentTable.Delete().Where(new Student()
        {
            studentName = "testtest", 
            studentAge = 20, 
        }).Commit();

### 事务的支持

Corm 支持事务，使用的时候，需要先利用 Corm 创建一个事务，然后将该事务作为方法参数，传入到具体的操作最后的 Commit() 方法当中

具体示例代码如下

    // 无事务支持，可成功插入
    studentTable.Insert()
        .Value(new Student() {studentName = "noneTrans"})
        .Commit();
    // 事务操作，
    using (var transaction = corm.BeginTransaction())
    {
        try
        {
            // 插入测试，如果事务无法完成的话，那么这行插入将无法成功
            studentTable.Insert()
                .Value(new Student() {studentName = "trans1"})
                .Commit(transaction);
            // 查找到 studentName 为 "oldName" 的行
            var list = studentTable.Find()
                .Where(new Student() {studentName = "oldName"})
                .Commit(transaction);
            // 将该行的 studentName 更新为 "newName"
            // 如果数据库当中不存在
            studentTable.Update()
                .Where(new Student() {studentName = list[0].studentName})
                .Value(new Student() {studentName = "newName"})
                .Commit(transaction);
            transaction.Commit();
        }
        catch (Exception e)
        {
            Console.WriteLine("发生异常：" + e.Message + " ，插入失败，事务回滚");
            transaction.Rollback();
        }
    }