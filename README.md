# FolderSync
基于MD5文件查重的类网盘式文件管理系统&amp;历史版本控制<br/>
其实就是有点仿git的托管方式<br/><br/>
*第一次用c#有点小紧张*<br/>

- Core Programming

规划的功能：
```
push <repo name> <local addr/name> [-m <msg>] [-root <addr>="/"] [-f]
pull <repo name> <local addr/name> [-root <addr>="/"] [-commit <SHA>|-i <index>] [-f]

repo create <repo name> <phys addr> [-desc <description>]
repo delete <repo name> [-rm <phys addr> [-q]]

repo property <repo name> <prop name> <prop value>

repo local add <local addr> <name>
repo local rename <old name> <new name>
repo local modify <new addr> <name>

delete <repo name> <SHA>|-i <index> [-f]
merge <repo name> <SHA>|-i <index> <SHA>|-i <index> [-m <msg>] [-f]
freeze <repo name> <SHA>|-i <index>
```
预计完坑时间: 寒假结束前
<p align="right">
Project 2016 - FolderSync
pandasxd
</p>
