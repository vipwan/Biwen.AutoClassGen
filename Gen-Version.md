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
你也可以引用外部`*.props`文件:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
	<Import Project="../Version.props"/>
</Project>
```
Version.props文件内容:
```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
	<PropertyGroup>
		<PackageVersion>2.0.0-preview4</PackageVersion>
		<PackageReleaseNotes></PackageReleaseNotes>
		<AssemblyVersion>2.0.3</AssemblyVersion>
		<FileVersion>2.0.2</FileVersion>
		<Version>2.0.1</Version>
	</PropertyGroup>
</Project>
```


#### 使用方式:
```csharp
Console.WriteLine({namespace}.Version.FileVersion);
Console.WriteLine({namespace}.Version.Current);
Console.WriteLine({namespace}.Version.AssemblyVersion);
```
#### 注意事项:

- 如果项目文件中没有设置版本号,则会自动创建一个版本号为1.0.0
