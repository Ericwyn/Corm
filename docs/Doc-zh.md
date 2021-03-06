
# Corm 中文文档
# 前言
Corm 是一个 C# 简易 orm 框架, 支持 SqlServer

支持 Entity 类创建，并根据 Entity 类自动建表及维护数据库表结构，针对诸多单表 SQL 操作进行了封装，还支持使用自定义 SQL 语句执行更加复杂的 SQL 操作，并且所有的 Corm 操作都支持 SQL 事务操作。


 - 源码地址 : [github.com/Ericwyn/Corm](https://github.com/Ericwyn/Corm)
 - 在线文档地址(最新文档)：[ericwyn.github.io/Corm/docs/Doc-zh.md](ericwyn.github.io/Corm/docs/Doc-zh.md)

# 使用说明
 - [快速开始](#快速开始)
     - [Attribute说明](#Attribute)
     - [Entity 类创建](#entity-类创建)
     - [Corm 初始化](#corm-初始化)
     - [使用 Corm 完成数据库操作](#使用-corm-完成数据库操作)
 - [数据表维护](#数据表维护)
     - [删除/创建/判断是否存在](#删除创建判断是否存在)
     - [自动维护表结构](#自动维护表结构)
 - [CURD 具体说明](#curd-具体说明)
     - [Select 查询操作](#select-查询操作)
        - Find All
        - 带 Where 的查询
        - 查询特定的字段
        - 只查询前 n 条数据
        - 查询首条数据
        - 判断 Select 查询的数据是否为空
        - Order By ASC | DESC
     - [Insert 插入操作](#insert-操作)
        - Insert 一条数据
        - Insert 多条数据
     - [Update 更新操作](#update-操作)
     - [Delete 删除操作](#delete-操作)
        - 删除全部数据
        - 按照特定条件删除
     - [自定义 SQL 语句操作](#自定义-sql-语句操作)
     - [SqlDataReader 解析](#自定义对-sqldatareader-的解析)
 - [事务的支持](#事务的支持)
 - [其他工具](#其他工具)
 - [TODO](#TODO)

## 快速开始

# Attribute
Corm 支持以下 `Attribute` 支持的属性如下

- `[Table(TableName)]`
    - 在 Entity 类上标记数据库的名称
    - `TableName` String 类型，描述该表的表名
- `[Column(Name, Size, SqlDbType, NotNull, PrimaryKey)]`
    - 在 Entity 类的属性当中标记数据库的列
    - `Name` String 类型，代表字段名称
    - `Size` int 类型，代表字段长度，非字符类型时候可不设置
    - `DbType` SqlDbType 类型，代表字段的数据类型
    - `NotNull` 代表非空，不设置时候默认为 false
    - `PrimaryKey` 代表主键字段，不设置时候默认为 false

### Entity 类创建
    
    [Table(TableName = "Student")]
    public class Student
    {
        [Column(Name = "studentName_", Size = 10,DbType = SqlDbType.VarChar)]
        public string studentName { get; set; }
        [Column(Name = "studentAge_", Size = 2, DbType = SqlDbType.Int)]
        public int? studentAge { get; set; }
    }

 - 需要使用可空类型，例如 int?、double?、bool? 、DateTime? TimeSpan?，否则 Where 查询会有错误
 - **Entity类的属性都需要将get 和 set 都写为 public**，否则无法使用反射 set 和 get 到具体的 value
 - 常用类型请设置成以下的 `SqlDbType`
    
    | C# | SqlDbType |
    | ------ | ------ |
    | int? | SqlDbType.Int |
    | string | SqlDbType.VarChar |
    | DateTime? | SqlDbType.DateTime |
    | DateTime? | SqlDbType.Date |
    | TimeSpan? | SqlDbType.Time |
    | ...   | ... |
            
### Corm 初始化

Corm 使用一个 Builder 来初始化，示例代码如下
    
    var corm = new Corm.CormBuilder()
        .Server("server=127.0.0.1;database=corm;uid=TestAccount;pwd=TestAccount")
        .SqlPrint(new CustomSqlPrintCb())
        .SyncTable(true)
        .Build();

Builder 支持以下方法

 - `Server()` 传入一个 SqlConnectStr，设置数据库连接信息
 - `SqlPrint()` 传入一个 SqlPrintCb 类，自定义设置的 Sql 日志打印
 - `SyncTable()`  传入一个 bool ，设置是否自动维护数据库表结构
 - `Build()` 创建一个 Corm 对象 


### 创建 CormTable 并操作数据库

    public static void Main(string[] args)
        {
            // 创建数据库连接
            var corm = new Corm.CormBuilder()
                .Server("server=127.0.0.1;database=corm;uid=TestAccount;pwd=TestAccount")
                .SqlPrint(new CustomSqlPrintCb())
                .Build();
 
            // 创建 CormTable 映射数据库当中的表
            var studentTable = new CormTable<Student>(corm);
            // CURD
            List<Student> studentTable.Find().Commit();
        }

## 数据表维护
### 删除/创建/判断是否存在
 - `CormTable<T>.DropTable()` 可删除表
 - `CormTable<T>.CreateTable()` 可创建表
 - `CormTable<T>.Exist()` 可查看表是否存在

        // 判断表是否存在
        if (studentTable.Exist())
        {
            // 删除表
            studentTable.DropTable();
            // 重新建表
            studentTable.CreateTable();
        }
        else
        {
            // 建表
            studentTable.CreateTable();
        }
        
### 自动维护表结构
 Corm 支持自动维护表结构，只需要在使用 CormBuilder 创建 Corm 的时候，调用 SyncTable() 方法，传入 true 参数就可以
 
 自动维护说明时候遵循一下规则
 
 - 如果数据表不存在，那么数据表将会被创建
 - 如果数据表存在，Corm 将会把 Entity 类和当前已有的数据表的表结构进行对比
    - 如果当前的 Entity 类存在某个字段，是已有数据表中没有的，那么该字段将被加入
    - 添加的时候，只会设置新字段的 (名称, 属性, 长度, 非空),不会设置其他信息 (例如主键)
 - 其余情况将不对数据库已有表结构做任何修改

 对于一个 CormTable<T> ，其绑定的数据表，最多只会在整个程序的运行过程中被自动同步一次表结构，所以无需担心创建多个 CormTable<Entity> 被多次创建的时候，Entity 绑定的表也被多次同步。


## CURD 具体说明
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
 
    更加复杂的一些 Where 条件查询，可以使用下面提到 WhereQuery 方法

 - 复杂 Where 查询
 
    为了使得 Where 查询获得更高的自由度，包括多条件的 AND/OR 查询，以及对 `<`, `>`, `LIKE` 这些条件符号提供支持， Corm 支持自定义使用`WhereQuery()` 方法设定更加复杂的 Where 查询语句及参数
    
    `WhereQuery()` 方法接受两个参数，第一个参数是 Where 条件语句，第二个是一个 SqlParameter 数组，参数二可为空
  
    示例代码如下
        
        // SELECT * FROM Student WHERE studentName_ like '%me%' and studentAge_ > 5  ;
        list = studentTable.Find().WhereQuery("studentName_ like '%me%' and studentAge_ > @StudentAge", new[]
        {
            new SqlParameter("@StudentAge", 5),
        }).Commit();
        Console.WriteLine(list.Count);
 
    **注意：WhereQuery 方法和 Where 方法不可同时使用**

 - 设定需要查询的字段
        
        List<Student> list = studentTable.Find().Attributes(new[] {"studentName_"}).Commit();

 - 只查询前 n 条，可使用 Top(n) 方法进行限定
    
        List<Student> list = studentTable.Find().Top(1).Commit();
 
 - 查询首条数据
 
    因为比较常用所以单独变成一个方法，就是对 Top(1) 和 Commit() 方法的封装
    
        Student = studentTable.Find().CommitForOne();
  
 - 判断 Select 查询的数据是否为空
    
    也是因为比较常用所以进行封装，先使用 CommitForReader 获得 reader，然后返回 reader.HasRows ;
        
        // 判断是否存在特定条件的学生
        bool hasThisStudent = studentTable.Find().Where(new Student() {
            studentName = "inset2"
        }).CommitForHas();
        Console.WriteLine(hasThisStudent);
 
 - Order By ASC | DESC 排序
 
        // Order By ASC 排序
        var list = studentTable.Find().OrderBy(new string[] {"age"}).Commit();
        // Order By DESC 排序
        list = studentTable.Find().OrderDescBy(new string[] {"age"}).Commit();
 
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

 - Update 使用 Where() 方法设置需要 Update 的条件，使用 Value() 方法设置更新之后的值

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

 - 更加复杂的 Where 操作可以使用 `WhereQuery()` 方法进行设置，`WhereQuery()` 的相关说明请参照上文 Select 操作中的相关说明

        // 使用 WhereQuery 设置 UPDATE 条件
        // UPDATE Student SET studentSex_='男' WHERE studentName_ like '%name%' and studentAge_ > 2 ;
        studentTable.Update().WhereQuery("studentName_ like @NameQuery and studentAge_ > @StudentAge ", new []
        {
            new SqlParameter("NameQuery", "%name%"),
            new SqlParameter("StudentAge", 2), 
        }).Value( new Student(){studentSex = "男"} ).Commit();

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

 - 更加复杂的 Where 操作可以使用 `WhereQuery()` 方法进行设置，`WhereQuery()` 的相关说明请参照上文 Select 操作中的相关说明

        // 使用 WhereQuery 设置 DELETE 条件
        // DELETE FROM Student  WHERE studentAge_ > 20 ; 
        studentTable.Delete().WhereQuery("studentAge_ > @StudentAge", new[]
        {
            new SqlParameter("StudentAge", 20),
        }).Commit();
        
### 自定义 SQL 语句操作

Corm 支持自定义 SQL 语句操作，使用 Customize() 方法可以创建一个 CormCustomizeMiddleSql 对象，该对象主要有以下方法

 - `SQL()`
    - 传入原生 SQL 语句和 SqlParameter 列表,
    - 第一个参数是 SQL 语句，第二个参数是 SqlParameter 的列表或者数组
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


 - 可以创建一个代理方法，来完成对一行 SqlDataReader 的解析，以此弥补自动反射时候，效率的不足， 代理方法定义如下
    
        // 方法接受一行 SqlDataReader ，将其转换成一个 T 返回
        public delegate T ReaderParseCb(SqlDataReader reader);

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

## TODO 
Corm 使用较为暴力的方法，针对“手写 SQL 并使用原生 C# SqlConnect 进行操作”的方式进行了封装，虽然提升了编码效率但是并没有提升 SQL 操作的效率，而且还因为使用了反射的操作，序列化 SqlDataReader 存在天生的性能缺陷，以下是整个 Corm 可能进行改进的地方

 - 数据库连接池？
 - 使用更高效的 SqlDataReader 序列化方法
 - 使用 SQL 语句构造树替代
 - JOIN 查询
 - 主键外键支持
 - ......