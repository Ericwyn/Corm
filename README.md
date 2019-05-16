# Corm
一个 C# 简易 orm 框架

# 原理

 - Corm (绑定数据库信息)
 - CormTable (基于 Corm，绑定 Table - Entity)
     - 直接使用 CormTable 的 CURD
     - CormTable 会根据绑定的 Entity ，创建 CURD 的数据库语句
        - CormTable 调用 FindAll 方法
        - Find 方法创建原始 MiddleSql（Select 专用）
        - 调用 MiddleSql 的 limit 之类的方法
            - limit
            - attributes
            - 排序
     - CormTable 完成对对象的封装等功能
 

# 特性 Attribute
## `[CormColumn(Name, Length, SqlDbType)]`
 - 数据库的列

# 使用说明
## 快速开始
### Entity 类创建

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
            var studentTable = new CormTable<Student>(corm, "Student");
            List<Student> studentTable.Find().All().Commit();
        }

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
        
 
