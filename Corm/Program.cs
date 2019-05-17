using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using Corm.attrs;

namespace Corm
{
    [CormTable(TableName = "Student")]
    public class Student
    {
        [CormColumn(Name = "studentName_", Size = 10,DbType = SqlDbType.VarChar)]
        public string studentName { get; set; }
        [CormColumn(Name = "studentAge_", Size = 2, DbType = SqlDbType.Int)]
        public int? studentAge { get; set; }
    }
    
    internal class Program
    {
        public static void Main(string[] args)
        {
            var corm = new Corm("server=127.0.0.1;database=corm;uid=TestAccount;pwd=TestAccount");
            var studentTable = new CormTable<Student>(corm);

            
            // SELECT 
            var students = studentTable.Find().All().Commit();
            Console.WriteLine(students.Count);
            

            /*
            // 按 where 条件查询
            var st = new Student();
            st.studentAge = 10;
            var students = studentTable.Find().Where(st).Commit();
            Console.WriteLine(students.Count);
            */
            
            /*
            // 查询特定的属性
            var list = studentTable.Find().Attributes(new[] {"studentName_"}).Commit();
            Console.WriteLine(list.Count);
            */
            
            /*
            // INSERT 插入，可插入 list 或者 单条数据，插入数据带有事务性质
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
            studentTable.Insert().Value(new List<Student>(){insert1,insert2}).Commit();
            studentTable.Insert().Value(insert1).Commit();
            */

            /*
            // UPDATE 更新，以 Where 作为过滤规则，以 Value 作为更新的值
            // 以下命令生成的 SqlCommand 语句为
            // UPDATE Student SET studentAge_=@studentAge_VALUE WHERE studentName_=@studentName_OLD ;
            // 相当于 Sql 
            // UPDATE Student SET studentAge_ = 20 WHERE studentName_ = 'testtest'
            studentTable.Update().Where(new Student()
            {
                studentName = "testtest",
            })
            .Value(new Student()
            {
                studentAge = 20,
            })
            .Commit();
            */
            
            /*
            // Delete 删除操作
            // 删除该表全部数据
            studentTable.Delete().All().Commit();
            // 删除所有 studentName 为 "testtest" , studentAge 为 20 的行
            studentTable.Delete().Where(new Student()
            {
                studentName = "testtest", 
                studentAge = 20, 
            }).Commit();
            */
            
        }
    }
}