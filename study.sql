# SQL的执行顺序
# from -> where -> group by -> having -> select -> order by -> limit

# 标量子查询
# 查询销售部的所有员工
select * from t_emp where dept_id = (select id from t_dept where name="销售部");

# 列子查询
# 常用操作符：IN、NOT IN、ANY、SOME、ALL
# 1.查询销售部或市场部的所有员工
select * from t_emp where dept_id in (select id from t_dept where name in ("销售部","市场部"));

# 2.查询比财务部的所有员工的工资都高的员工
select * from t_emp where salary > all(select salary from t_emp where dept_id = (select id from t_dept where name = "财务部"));

# 3.查询比财务部的任一员工的工资高的员工
select * from t_emp where salary > any(select salary from t_emp where dept_id = (select id from t_dept where name = "研发部"));

# 行子查询
# 常用操作符：=、<>、IN、NOT IN
# 1.查询与“卢科”的城市和直属领导先通的员工
select * from t_emp where (city, manager_id) = (select city, manager_id from t_emp where name="卢科");

# 表子查询
# 常用操作符：IN
# 1.查询与“汪瑜“和“王小焕”的性别和薪资相同的员工
select * from t_emp where (gender, salary) in (select gender, salary from t_emp where name in ("汪瑜", "王小焕"));

# 2.查询出生日期1990-01-01之后的员工信息及其部门信息
select e.*, d.* from (select * from t_emp where birth > "1990-01-01") e left join t_dept d on e.dept_id = d.id;

# 加强练习

# 查询员工的信息及其部门信息（隐式内连接，就是多个表之间用逗号分隔）
select e.*, d.name from t_emp e, t_dept d where e.dept_id=d.id;

# 查询年龄>1990的员工信息及其部门信息（显式连接）
select e.*, d.name from t_emp e join t_dept d on e.dept_id=d.id where e.birth>'1990-01-01';

# 查询有员工的部门的信息
select distinct d.* from t_dept d, t_emp e where d.id=e.dept_id;

# 查询年龄>1990的员工信息，员工没有部门信息也要查询出来
select e.*, d.name from t_emp e left join t_dept d on e.dept_id=d.id where e.birth>'1990-01-01';

# 查询员工的薪资等级
select e.*, s.grade from t_emp e, t_salary_grade s where e.salary >= s.low and e.salary <= s.high;
select e.*, s.grade from t_emp e, t_salary_grade s where e.salary between s.low and s.high;

# 查询研发部所有员工的信息及其工资等级
select e.*, s.grade, d.name from t_emp e, t_salary_grade s, t_dept d where (e.salary between s.low and s.high) and e.dept_id = d.id and d.name="研发部";

# 查询研发部所有员工的平均工资
select avg(e.salary) from t_emp e, t_dept d where e.dept_id = d.id and d.name="研发部";

# 查询工资比吴利平高的员工信息
select e.* from t_emp e where e.salary > (select e.salary from t_emp e where e.name="吴利平");

# 查询工资比平均工资高的员工信息
select e.* from t_emp e where e.salary > (select avg(e.salary) from t_emp e);

# 查询工资比本部门平均工资高的员工信息
select e.* from t_emp e, (select avg(salary) avg_salary, dept_id from t_emp group by dept_id) e2 where e.dept_id=e2.dept_id and e.salary>e2.avg_salary;

select e1.* from t_emp e1 where e1.salary > (select avg(e2.salary) avg_salary from t_emp e2 where e1.dept_id=e2.dept_id);

# 查询所有的部门信息，并统计部门员工人数
select d.*, t.* from t_dept d left join (select count(*), dept_id from t_emp group by dept_id) t on d.id=t.dept_id;

select d.*, (select count(*) from t_emp e where e.dept_id=d.id) emp_count from t_dept d;
