﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using Corm.attrs;

namespace Corm
{
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
            var studentTable = new CormTable<Student>(corm, "Student");

            /*
            // find All 
            var students = studentTable.Find().All().Commit();
            Console.WriteLine(students.Count);
            */

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
            // 插入，可插入 list 或者 单条数据，插入数据带有事务性质
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
            
            
        }
    }
}