# Corm
一个 C# 简易 orm 框架, 支持 SqlServer

支持自动维护数据库表结构，具体可查看 [自动维护表结构-说明](#自动维护表结构)

 - 文档： [中文文档](doc/Doc-zh.md)
 - 代码示例： [CormTest-Program](Corm/Program.cs)

# 原理

 - Corm 连接数据库，维护 SqlConnection
 - CormTable (基于已创建的 Corm，依据 Entity 类属性的 Attribute 绑定 Table - Entity )
 - 使用 CormTable 的 Find、Insert、Update、Delete 方法创建 MiddleSql
	 - 具体 Sql 语句的构建交给 MiddleSql 类来完成，使用反射来分析需要插入、查找的对象的属性和值
	 - MiddleSql 的 Commit 执行具体的 SQL 操作
	 - MiddleSql 将 SQL 执行返回的数据再自动封装成 List<T> 返回 （SELECT 操作），还可以选择返回受影响行数、SqlDataReader
 - [Corm 具体设计思路](#设计思路)

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
   - CommitForHas()
        
        返回一个 bool ，表示是否存在以查询条件来限定的行
        
   - CommitForNone(CormTransaction trans)
   - CommitForList(CormTransaction trans)
   - CommitForReader(CormTransaction trans)
   - CommitForHas(CormTransaction trans)
 
 - DropTable()
    
    删除数据表
 
 - CreateTable()
    
    创建数据表
 
 - Exist()
 
    判断表是否存在
    
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

# 设计思路


## 核心对象
 - Corm
 - CormTable
 - MiddleSql
    - CormInsertMiddleSql
    - CormDeleteMiddleSql
    - CormUpdateMiddleSql
    - CormSelectMiddleSql
    - CormCustomizeMiddleSql
 - CormTransaction

Corm 的设计思路其实非常的简单，就是 “SQL语句构造器” + “SQL 执行”

其中各个 MiddleSql 承担的就是 SQL 语句构造器的任务（最后也包含了 SQL 执行，设计上来讲还是觉得 SQL 的执行应该同 Sql 语句的创建解耦）

其他的例如 Corm\CormTable 以及各种 Utils ，都只是为 SQL 执行提供条件（例如提供 SqlConnect），而`Table` 和 `Column` 这两个 Attributes 则是为了方便生成 SQL 语句 

## 核心对象说明
### Corm
 - 管理 Sql 连接

### CormTable
 - 每一个 CormTable 对应的就是数据库当中的一张表
 - 其创建需要基于 Corm 对象，因为 CormTable 本身并不管理和 Sql 连接，所有连接都需要从 Corm 处获取
 - CormTable 主要负责完成对 MiddleSql 对象的创建，然后由 MiddleSql 去设置具体 CURD 操作应该怎么做，并且完成这些操作
 
    - Find()      --> 创建 CormSelectMiddleSql 对象
    - Insert()    --> 创建 CormInsertMiddleSql 对象
    - Update()    --> 创建 CormUpdateMiddleSql 对象 
    - Delete()    --> 创建 CormDeleteMiddleSql 对象
    - Customize() --> 创建 CormCustomizeMiddleSql 对象
 
### MiddleSql
 - 一个 MiddleSql 完成的就是一行 Sql 语句的创建，并执行这个 Sql 语句最终返回结果
 - 因为CURD 增删改查操作的差异，所以 Sql 创建操作并不是通用的，所以会区分出 5 类的 MiddleSql
 - 接受并缓存用户对于具体 Sql 操作的设置，例如 Select 可以设置 `Top` ，`WhereList` ，`Order By` 这些设置
 - 最后用户调用 Commit 方法的时候，MiddleSql 为用户生成最终的 Sql 语句，并且执行

### CormTransaction

因为对于事务的支持，其重点就是多个 `sqlCommand.ExecuteNonQuery()` 或者 `sqlCommand.ExecuteReader()`，都是在同一个 `SqlTransaction` 上面创建的，而同一个 `SqlTransaction` 只能来源于同一个 `SqlConnection`。

所以，如果要达成事务操作，需要先创建一个共用的 `SqlTransaction`，也就是需要先创建一个共用的 `SqlConnection`。而 `CormTransaction`，实际上就是用来管理这共用的 `SqlConnection` 与 `SqlTransaction` 的

于是在使用 `CormTable.BeginTransaction` 的时候，会传入该 `CormTable` 带有的 `Corm` 对象，基于 `Corm` 对象的 `NewConnection` 来创建 `CormTransaction` 所需要的 `SqlConnection` 以及  `SqlTransaction`（这部分可以查看 `CormTransaction` 的构造函数）

而后，就是把 `CormTable` 当中 `MiddleSql` 构造好的 Sql ，交由 `CormTransaction` 来创建 `SqlCommand`，不过其实具体的 `SqlCommand` 的创建、使用， 和 `SqlParameter` 的设置，是封装到另一个类 `CormTransSql` 来完成的，`CormTransaction` 只负责接受 `SqlString` 和 `SqlParameter`，以及管理 `SqlTransaction` 的提交和回滚，而最关键的 `ExecuteNonQuery()` / `ExecuteNonQuery()` 也是在 `CormTransSql` 而非 `CormTransaction` 里面完成的

 - 不需要事务操作的时候，`SqlCommand` 的创建和执行，都是在 `CormTable` 创建的 `MiddleSql` 对象里面完成的
 - 需要事务操作的话，就是吧 `MiddleSql` 创建好的 `SQL` 语句，统一交到一个 `CormTransaction` 里面执行
最后，`CormTransaction` 封装 `SqlTransaction` 的 `RollBack()` 和 `Commit()` 方法，让用户操控事务的提交和回滚

## 自动维护表结构
 Corm 支持自动维护表结构，只需要在使用 CormBuilder 创建 Corm 的时候，调用 SyncTable() 方法，传入 true 参数就可以
 
### 自动维护说明
 - 如果数据表不存在，那么数据表将会被创建
 - 如果数据表存在，Corm 将会把 Entity 类和当前已有的数据表的表结构进行对比
    - 如果当前的 Entity 类存在某个字段，是当前数据表没有的，那么该字段将被加入
 - 其余情况将不对数据库已有表结构做任何修改