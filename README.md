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
## 核心功能
 - Find()
    
    查询操作
    
    - Attributes(string[] colunmNames)
      - 只查询特定字段
    - Where(T whereObj)
      - Where 条件查询
    - WhereLike(string colunmnName, string likeQuery)
      - Like 条件查询
    - OrderBy(string[] atts)
    - OrderDescBy(string[] atts)
      - Order By 条件设置, 多个条件使用 and 连接
    - Top(int num)
      - 只查询 Top N 条
    - Commit(CormTransaction trans = null)
    - CommitForOne(CormTransaction trans = null)
    - CommitForReader(CormTransaction trans = null)
      - Sql操作提交，提供不同的放回
 
 - Insert()
 
    插入操作
 
   - Value(T entity)
   - Value(List<T> entityList)
   - Commit()
   - Commit(CormTransaction trans)
 
 - Update()
 
    更新操作
 
   - Where(T whereObj)
   - Value(T obj)
   - Commit()
   - Commit(CormTransaction trans)

 - Delete()
 
    删除操作
    
   - Where(T whereObj)
   - All()
   - Commit()
   - Commit(CormTransaction trans)

 - Customize()
 
    自定义SQL操作
    
   - SQL(string sql)
   - SQL(string sql, List<SqlParameter> paramList)
   - CommitForNone()
   - CommitForList()
   - CommitForReader()
   - CommitForNone(CormTransaction trans)
   - CommitForList(CormTransaction trans)
   - CommitForReader(CormTransaction trans)

## 其他功能
 - 事务操作
 - SqlDataReaderParse<T>
    - SqlDataReader 解析工具
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