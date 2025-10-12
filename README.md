# Biwen.AutoClassGen

![Nuget](https://img.shields.io/nuget/v/Biwen.AutoClassGen)
![Nuget](https://img.shields.io/nuget/dt/Biwen.AutoClassGen)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/LICENSE.txt) 
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/vipwan/Biwen.AutoClassGen/pulls) 

Biwen.AutoClassGen 是一个代码生成工具库，通过源代码生成器（Source Generator）技术自动创建常用代码模式，简化开发流程，提高工作效率。

## 主要功能

- **DTO自动生成**：自动从实体类生成DTO对象，并创建映射扩展方法
- **Curd服务生成**：为标注了特性的实体类生成常见的CRUD服务接口与实现
- **请求对象生成**：快速生成具有相同字段的请求对象族，减少重复工作
- **AOP装饰器模式**：自动实现装饰器模式，无需手动编写大量样板代码
- **依赖注入自动注册**：通过简单的特性标记，自动注册服务到DI容器
- **版本信息生成**：自动生成程序集版本信息
- **元数据生成**：自动生成程序集元数据
- **代码分析与修复**：提供多种代码分析规则和自动修复功能
- **枚举描述生成**：根据枚举值自动生成描述信息(`Description`,`Display`)，方便在UI中显示

[中文文档](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/README-zh.md)

## AutoCurd（自动生成 CURD 服务）

该功能可以为标注了 `AutoCurd` 特性的实体类型自动生成常见的 CRUD 服务接口与实现（例如 `IUserCurdService` / `UserCurdService`），减少手写样板代码。

重要行为说明：
- 生成器只会为显式标注了 `AutoCurd` 特性的实体生成服务。即必须把特性放到实体类型本身上，才会生成对应的接口与实现。
- 分析器会在编译时校验 `AutoCurd` 的泛型参数是否为 `Microsoft.EntityFrameworkCore.DbContext` 的派生类型；若不是，会产生错误诊断 `GENCURD001`。

特性使用方式：

- 泛型形式（C# 11+ 泛型特性）

```csharp
[AutoCurd<MyDbContext>("Your.Target.Namespace")]
public partial class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

生成规则和约定：
- `Target.Namespace`（字符串）参数用于指定生成的接口/实现所在的命名空间。如果不指定或为空，生成器会使用程序集名称作为回退命名空间。
- 为 `User` 生成的代码示例：
  - 接口：`IUserCurdService`（包含 `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetAsync`）
  - 实现：`UserCurdService`（简单基于 `DbContext.Set<User>()` 的实现，方法为 `virtual`）
- 如果你在代码中引用了这些生成的类型（例如在 `Program.cs` 中注入 `IUserCurdService`），请确保对应实体已标注 `AutoCurd`，否则编译器会找不到生成类型并导致构建失败。

诊断与错误：
- `GENCURD001`：当 `AutoCurd` 的泛型参数不是 `DbContext` 的派生类型时触发（例如 `[AutoCurd<User>("ns")]`），分析器将在特性处或类型处报告错误，说明需要传入正确的 `DbContext` 类型。

示例：

```csharp
// 正确：DbContext 类型
[AutoCurd<MyAppDbContext>("MyApp.Services.Curd")] 
public partial class Product { ... }

// 错误：User 不是 DbContext，将触发 GENCURD001
[AutoCurd<User>("MyApp.Services.Curd")] 
public partial class Order { ... }
```

## 快速开始

### 安装

```xml
<ItemGroup>
	<PackageReference Include="Biwen.AutoClassGen.Attributes" Version="x.x.x" />
	<PackageReference Include="Biwen.AutoClassGen" Version="x.x.x" PrivateAssets="all" />
</ItemGroup>
```

### 主要功能示例

#### 1. DTO自动生成
通过简单的特性标记，自动生成DTO类和映射方法：
```csharp
[AutoDto(typeof(User))]  // 或使用 C# 11+ 的泛型特性: [AutoDto<User>]
public partial class UserDto
{
    // 属性会自动从User类生成
}
```

#### 2. 装饰器模式自动实现
轻松实现AOP装饰器模式：
```csharp
[AutoDecor<LoggingDecorator>]
public interface IUserService 
{
    Task<User> GetUserAsync(int id);
}
```

#### 3. 依赖注入自动注册
自动注册服务到DI容器：
```csharp
[AutoInject<IUserService>(ServiceLifetime.Scoped)]
public class UserService : IUserService
{
    // 实现代码
}
```

#### 4. 版本信息自动生成
自动生成程序集版本信息，支持git版本号：
```csharp
Console.WriteLine($"Version: {XXX.Generated.Version}");
Console.WriteLine($"Version: {XXX.Generated.AssemblyMetadata}");
```

#### 5. 枚举描述自动生成
```csharp
[AutoDescription]
public enum UserStatus
{
    [Description("正常")]
    Normal,
    [Description("禁用")]
    Disabled,
    [Display(Name = "已删除")]
    Deleted
}
// 生成的描述信息
Console.WriteLine(UserStatus.Normal.Description()); // 输出: 正常
Console.WriteLine(UserStatus.Deleted.Description()); // 输出: 已删除

```


### 详细文档

以下是每个功能的详细使用说明：

- [DTO生成器使用文档](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-Dto.md)
- [请求对象生成器文档](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-request.md)
- [装饰器模式生成器文档](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-Decor.md)
- [依赖注入自动注册文档](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-AutoInject.md)
- [版本信息生成器文档](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-Version.md)
- [程序集元数据生成器文档](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-Metadata.md)
- [枚举描述生成生成器文档](https://github.com/vipwan/Biwen.AutoClassGen/issues/11)

- [更多文档...](https://github.com/vipwan/Biwen.AutoClassGen/issues?q=is%3Aissue%20state%3Aopen%20label%3Adocumentation)

### 代码分析器功能

该库提供了一系列代码分析器，帮助您编写更规范、更高质量的代码：

- `GEN001` : 检查接口继承关系，确保能正确生成实现类
- `GEN011` : 防止生成类与接口名称冲突
- `GEN021` : 建议使用统一的命名空间以提高代码组织性
- `GEN031` : 提示使用 `[AutoGen]` 特性实现自动生成

#### DTO生成器规则
- `GEN041` : 检测重复的 `[AutoDto]` 特性标注
- `GEN042` : 禁止在抽象类上使用 `[AutoDto]` 特性
- `GEN044` : `[AutoDto]` 无法解析目标类型，请确保引用了正确的程序集并且类型可访问
- `GEN045` : 确保标记 `[AutoDto]` 的类声明为 partial

#### 装饰器相关规则
- `GEN043` : 确保被 `[AutoDecor]` 标记的类型具有public访问级别

#### 代码风格规则
- `GEN050` : 检查文件是否包含必要的头部注释信息
- `GEN051` : 确保异步方法名称以Async结尾
- `GEN052` : 推荐使用C# 10的文件作用域命名空间声明
- `GEN053` : 确保源代码文件使用UTF-8编码

### 自动代码修复

本库提供多个自动代码修复功能，帮助您快速修正代码问题：

- ✨ 自动添加缺失的文件头部注释
- 🔄 将异步方法名称规范化（添加Async后缀）
- 🎯 自动应用 `[AutoGen]` 特性
- 🔍 移除重复的特性标注
- 📝 转换为文件作用域命名空间
- 🛠 更多代码修复持续添加中...

### 使用该库的项目

以下是一些使用Biwen.AutoClassGen的优秀项目：

- [Biwen.QuickApi](https://github.com/vipwan/Biwen.QuickApi) - 快速API开发框架

如果您的项目正在使用本库，欢迎通过PR将您的项目添加到此列表！

### 参与贡献

欢迎提交Pull Request来改进这个项目！无论是修复bug、添加新功能，还是完善文档，我们都非常感谢您的贡献。

### 开源协议

本项目采用MIT协议开源，详见 [LICENSE](LICENSE.txt) 文件.
