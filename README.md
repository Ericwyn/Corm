## Corm
一个 C# 简易 orm 框架

## 原理

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
 

## 特性 Attribute
### `[CormColumn(Name, Length, SqlDbType)]`
 - 数据库的列