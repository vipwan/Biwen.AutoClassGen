## 自动创建程序集版本信息

#### 项目文件设置版本号:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
	<PropertyGroup>
		<Version>1.0.0</Version>
		<FileVersion>1.0.0</FileVersion>
	</PropertyGroup>
</Project>
```

#### 使用方式:
```csharp
Console.WriteLine({namespace}.Version.FileVersion);
Console.WriteLine({namespace}.Version.Current);
```
#### 注意事项:

- 如果项目文件中没有设置版本号,则会自动创建一个版本号为1.0.0
