# Corm
一个 C# 简易 orm 框架, 支持 SqlServer

不支持自动维护数据库表结构，使用的时候，先设计好数据库，之后依据数据库的表结构创建 Entity 类

 - 文档： [中文文档](doc/Doc-zh.md)
 - 代码示例： [CormTest-Program](Corm/Program.cs)

# 原理

 - Corm 连接数据库，维护 SqlConnection
 - CormTable (基于已创建的 Corm，依据 Entity 类属性的 Attribute 绑定 Table - Entity )
 - 使用 CormTable 的 Find、Insert、Update、Delete 方法完成增删改查
	 - 具体 Sql 语句的构建交给 MiddleSql 类来完成，反射分析需要插入、查找的对象的属性和值
	 - MiddleSql 类会将查询到的数据再自动封装成 List<T> 返回 

# 功能列表
 - 查找 Select
	 - Where 条件
		- 字符串 Like 条件
	 - 只查找部分字段
	 - 将返回结果的自动封装成 List<T>
	 - 手写 Sql 语句进行查询(同样支持事务)
	 - 只查询 Top n 条
	 - Order By ASC | DESC 排序
     - ~~分页 查询~~
     - ~~Join 查询~~
 - 添加 Insert
	 - 添加单条数据
	 - 添加多条数据
 - 更新 Update
	 - Where 设定更新过滤
	 - Value 设定替换的新内容
 - 删除 Delete
     - 删除全部
	 - 按特定条件删除
 - 支持事务
 - ~~启动时候对 Entity 结构进行判断~~
 - 自动解析 SqlDataReader
    - 支持自定义解析类型
 - 自定义 Sql 和日志打印的回调
 - Build模式，初始化和创建

# 快速开始


    namespace CormTest
    {
        /*
        * Entity 类
        */
        [Table(TableName = "Student")]
        public class Student
        {
            [Column(Name = "studentName_", Size = 10,DbType = SqlDbType.VarChar)]
            public string studentName { get; set; }
            
            [Column(Name = "studentAge_", Size = 2, DbType = SqlDbType.Int)]
            public int? studentAge { get; set; }
        }
        
        internal class Program
        {
            public static void Main(string[] args)
            {
                var corm = new Corm.CormBuilder()
                    .Server("server=127.0.0.1;database=corm;uid=TestAccount;pwd=TestAccount")
                    .SqlPrint(new CustomSqlPrintCb())
                    .Build();
                var studentTable = new CormTable<Student>(corm);
    
                // SELECT * FROM Student
                var students = studentTable.Find().All().Commit();
                Console.WriteLine(students.Count);
            }
        }
    }