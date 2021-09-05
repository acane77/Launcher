# SAMPLE CONFIG FILE

```
[SYSTEM]
Title=TBM服务启动器

[program_1]
Name=数据采集和实时数据提供
Command=tbm.exe -p {port} -D tbm.db
Port=7706
Requirement_Command=tbm.exe -v
Status_Check_Method=PORT_USAGE

[program_2]
Name=实时采集数据可视化
Command=http-server.cmd ./webclient/ -p {port} -P http://localhost:7706
Port=8080
Requirement_Command=http-server.cmd -v
Status_Check_Method=PORT_USAGE

[program_3]
Name=旧数据提供
Command=tbm.exe -p {port} -D new.db --disable-collector
Port=7707
Requirement_Command=tbm.exe -v
Status_Check_Method=PORT_USAGE

[program_4]
Name=Chrome
Command=test\OnlineSearch.exe
Requirement_Command=CHECK_EXISTANCE
Status_Check_Method=EXECUTABLE_EXISTANCE
```