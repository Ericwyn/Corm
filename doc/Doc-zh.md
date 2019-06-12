
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
    
    [Table(TableName = "Student")]
    public class Student
    {
        [Column(Name = "studentName_", Size = 10,DbType = SqlDbType.VarChar)]
        public string studentName { get; set; }
        [Column(Name = "studentAge_", Size = 2, DbType = SqlDbType.Int)]
        public int? studentAge { get; set; }
    }

 - 需要使用可空类型，例如 int?、double?、bool? ，否则 Where 查询会有错误
 - **Entity类的属性都需要将get 和 set 都写为 public**，否则无法使用反射 set 和 get 到具体的 value

### 使用 Corm 完成数据库操作

    public static void Main(string[] args)
        {
            var corm = new Corm.CormBuilder()
                .Server("server=127.0.0.1;database=corm;uid=TestAccount;pwd=TestAccount")
                .SqlPrint(new CustomSqlPrintCb())
                .Build();
            var studentTable = new CormTable<Student>(corm);
            List<Student> studentTable.Find().Commit();
        }

 - Corm 由 Corm.CormBuilder类创建，需要传入 SqlConnection String，可以传入 SqlPrintCB 自定义 sql 打印 
 - CormTable 创建的时候可以手动设置对应的表的名字
 - 也可使用 `[CormTable(TableName="xxx")]` 来标记 Entity 类对应的表的名称，无需在创建时候传入

## 具体说明
### Select 查询操作
 - Find All
    
        List<Student> list = studentTable.Find().Commit();
 
 - 带 Where 的查询
        
        // 使用一个对象存储 Where 条件
        var selectTemp = new Student();
        // 可使用多个条件来限定
        // selectTemp.studentAge = 10;
        selectTemp.studentName = "aaa";
        List<Student> list = studentTable.Find().Where(selectTemp).Commit();
 
 - 设定需要查询的字段
        
        List<Student> list = studentTable.Find().Attributes(new[] {"studentName_"}).Commit();

 - 只查询前 n 条，可使用 Top(n) 方法进行限定
    
    此处命名可能会有误解，**`All()` 方法指的字段的 All ，而此处的 Top n 指的是查询行数** 

        List<Student> list = studentTable.Find().Top(1).Commit();
 
 - 查询首条数据
 
    因为比较常用所以单独变成一个方法，就是对 Top(1) 和 Commit() 方法的封装
    
        Student = studentTable.Find().CommitForOne();
 
 - Order By ASC | DESC 排序
 
        // Order By ASC 排序
        var list = studentTable.Find().OrderBy(new string[] {"age"}).Commit();
        // Order By DESC 排序
        list = studentTable.Find().OrderDescBy(new string[] {"age"}).Commit();
 
 - Like 查询
    
    Corm 支持简单的 Like 查询，只需要调用 WhereLike 方法就可以了，接受两个参数
     - 第一个参数是 column Name
     - 第二个参数是具体的 Like 的内容
     - 例如使用 `.WhereLike("studentName_", "test")` 将会在 Sql 语句当中拼接 `studentName_ LIKE '%test%'`
     - WhereLike 方法可多次调用，拼接的语句会使用 AND 进行连接
     
     示例代码如下
     
         // SELECT Like 查询
         // 将会使用 LIKE 语句如下
         // 
         //         studentName_ LIKE '%test%' AND studentAge_ LIKE '%2%'
         //
         var list = studentTable.Find()
             .WhereLike("studentName_", "test")
             .WhereLike("studentAge_", "2")
             .Commit();
         Console.WriteLine(list.Count);
 
 - 手写 Sql 语句进行查询
 
    Corm 支持传入手写的 Sql 语句进行查询，也会自动解析成 Entity 类的 List，并且也提供了事务的支持，可使用以下两个方法
        
    - `Customize(string sqlStr)`
        - 可传入拼接的 Sql
    - `Customize(string sqlStr, SqlParameter[] parameters)`
        - 传入预编译语句以及需要替换的参数
    
    示例代码如下
    
        // SELECT 自定义查询语句
        var list = studentTable.Customize().SQL(
        "SELECT * FROM Student WHERE studentName_=@studentName_",
        new SqlParameter[]
        {
            new SqlParameter("@studentName_", "test3"),
        })
        .Commit();     // 如需要使用事务的话可在此处传入 SqlTransaction 对象
        Console.WriteLine(list);
 
 - 更加原生的使用，可以使用 `CommitForReader()` 方法，配合 `Customize()` 和 `SqlDataReaderParse<T>.parse()`方法，传入自定义 Sql 语句，而后由用户自己对返回的 SqlDataReader 进行读取和解析
    - `CommitForReader()` 方法同样支持事务操作
 

 
### Insert 操作
Insert 方法使用 Value 传入需要插入的值，可为一个 Entity 的 List 或者一个单独的 Entity 对象

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

### 自定义 SQL 语句操作

Corm 支持自定义 SQL 语句操作，使用 Customize() 方法可以创建一个 CormCustomizeMiddleSql 对象，该对象主要有以下方法

 - `SQL()`
    - 传入原生 SQL 语句和 SqlParameter 列表
 - `CommitForNone()`
    - 不要求返回 DataReader 或者 List<T>，适用于 Update、Insert、Delete之外的操作，返回受影响的行数
 - `CommitForList()`
    - 返回 List<T>
 - `CommitForReader()`
    - 返回 DateReader

Customize() 操作和其他操作一样，也是支持事务的，具体的操作可以看文档后面关于事务的说明

示例代码如下

    // 直接返回 SqlDataReader
    // 并使用 SqlDataReaderParse 工具解析 reader
    var sqlTemp1 = @"SELECT 
                    studentName_ as name, 
                    studentAge_ as age 
                FROM Student ";
    SqlDataReader reader = studentTable.Customize().SQL(sqlTemp1).CommitForReader();
    List<TempStruct> listTemp = SqlDataReaderParse<TempStruct>.parse(reader, true, true);
    Console.WriteLine(listTemp.Count);

### 自定义对 SqlDataReader 的解析
    
Corm 可以帮助你把 SqlDataReader 解析成任何类型，只需要使用工具类 SqlDataReaderParse<T> 就好了
    
 - T 是临时创建的数据解析结构体，类似于 Entity 类，需要使用 `[Coloum]` 来标记属性，只需要注明其 Name 属性就可以来

         private class TempStruct
         {
             [Column(Name = "name")]
             public string Name { get; set; }
             [Column(Name = "age")]
             public string Age { get; set; }
         }

 - T 也可以不使用 `[Colunm]` 来标记，Reader 读取的时候会直接使用该属性的反射得到的 Name,作为 Reader 读取数据的 key

 - 配置自定义SQL语句查询的 CommitForReader() 方法使用，示例代码如下

         var sql = @"SELECT 
                         studentName_ as name, 
                         studentAge_ as age 
                     FROM Student ";
         SqlDataReader reader = studentTable.Customize().SQL(sql).CommitForReader();
         List<TempStruct> list = SqlDataReaderParse<TempStruct>.parse(reader, true); 



### 事务的支持

Corm 支持事务

当使用事务的时候，需要先通过 CormTable<T> 实例对象的 BeginTransaction() 创建一个 CormTransaction

CormTransaction 缓存一次事务操作中，多个 Sql 操作所共同需要的 SqlConnection 和 SqlTransaction

在事务都执行之后，使用 CormTransaction 对象的 Commit 方法进行事务提交，发生异常的话就使用 Rollback 方法回滚

具体示例代码如下

    // 事务操作示例
    using (CormTransaction transaction = studentTable.BeginTransaction())
    {
        try
        {
            studentTable.Insert()
                .Value(new Student() {studentName = "oldName"})
                .Commit(transaction);
            list = studentTable.Find()
                .Where(new Student() {studentName = "oldName"})
                .Commit(transaction);
            studentTable.Update()
                .Where(new Student() {studentName = list[0].studentName})
                .Value(new Student() {studentName = "newName"})
                .Commit(transaction);
            var sql = @"SELECT * FROM student WHERE studentName_= @studentName_";
            var list2 = studentTable.Customize()
                .SQL(sql,new[] {new SqlParameter("studentName_", "newName"),})
                .CommitForList(transaction);
            Console.WriteLine(list2.Count);
            transaction.Commit();
        }
        catch (Exception e)
        {
            Console.WriteLine("发生异常：" + e.Message + " ，插入失败，事务回滚");
            transaction.Rollback();
        }
    }

### 其他工具
CormUtils 封装了一些方法，用以更好的使用 Corm 框架
 - GetTableName 可以获取 Entity 类的表名   
 - GetProPropertyInfoMap 可以获取 Entity 类当中，所有字段的 Map
        
        Console.WriteLine(CormUtils<Student>.GetTableName());
        Console.WriteLine(CormUtils<Student>.GetProPropertyInfoMap().Count);