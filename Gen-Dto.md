# 🚀 AutoDto Generator - DTO自动生成器

[![Nuget](https://img.shields.io/nuget/v/Biwen.AutoClassGen)](https://www.nuget.org/packages/Biwen.AutoClassGen)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/LICENSE.txt)

AutoDto Generator 是一个源代码生成器，可以自动从实体类生成 DTO 对象并创建双向映射扩展方法。支持基础 DTO 和复杂嵌套 DTO 两种模式。

---

## 📋 使用方式 (Usage)

### 🔧 基础语法 (Basic Syntax)

```csharp
// .NET Standard 2.0+ 支持
[AutoDto(Type entityType, params string[] ignoredProperties)]

// C# 11 (.NET 7+) 泛型特性支持
[AutoDto<T>(params string[] ignoredProperties)]

// 复杂对象 DTO 支持嵌套生成
[AutoDtoComplex(int maxNestingLevel = 2)]
```

### 📊 DTO 模式对比 (Basic DTO vs Complex DTO)

| 特性 Feature | 基础 DTO (Basic) | 复杂 DTO (Complex) |
|-------------|-----------------|-------------------|
| **映射层级** | 🔹 单层属性映射 | 🔸 多层嵌套映射 |
| **性能** | ⚡ 快速，低内存占用 | 🔄 功能全面，开销较高 |
| **使用场景** | 📡 API 响应，简单数据传输 | 🏗️ 业务逻辑，复杂数据操作 |
| **生成内容** | DTO类 + 基础映射方法 | DTO类族 + 嵌套映射方法 |

---

## 🏗️ 实体定义 (Entity Definitions)

### 📝 简单实体示例 (Simple Entity Example)

```csharp
namespace Biwen.AutoClassGen.TestConsole.Entitys
{
    /// <summary>
    /// 用户实体 - User Entity
    /// </summary>
    public class User : Info
    {
        /// <summary>
        /// 用户ID - User ID
        /// </summary>
        public string Id { get; set; } = null!;
        
        /// <summary>
        /// 名字 - First Name
        /// </summary>
        public string FirstName { get; set; } = null!;
        
        /// <summary>
        /// 姓氏 - Last Name
        /// </summary>
        public string LastName { get; set; } = null!;
        
        /// <summary>
        /// 年龄 - Age
        /// </summary>
        public int? Age { get; set; }
        
        /// <summary>
        /// 全名 - Full Name
        /// </summary>
        public string? FullName => $"{FirstName} {LastName}";
    }

    /// <summary>
    /// 基础信息 - Basic Information
    /// </summary>
    public abstract class Info : Other
    {
        /// <summary>
        /// 邮箱 - Email Address
        /// </summary>
        public string? Email { get; set; }
    }

    /// <summary>
    /// 其他信息 - Other Information
    /// </summary>
    public abstract class Other
    {
        /// <summary>
        /// 备注 - Remark
        /// </summary>
        public string? Remark { get; set; }
    }
}
```

### 🔗 复杂实体示例 (Complex Entity Example)

```csharp
/// <summary>
/// 复杂对象示例 - Complex Object Example
/// 展示多层嵌套和集合属性 - Demonstrates multi-level nesting and collection properties
/// </summary>
public class Person
{
    [Required]
    [Display(Name = "姓名")]
    public string Name { get; set; } = string.Empty;

    [Required, Range(0, 200)]
    [Display(Name = "年龄")]
    public int Age { get; set; }

    /// <summary>
    /// 嵌套对象 - Nested Object
    /// </summary>
    public Address Address { get; set; } = new Address();

    /// <summary>
    /// 集合属性 - Collection Property
    /// </summary>
    public List<Hobby> Hobbies { get; set; } = [];

    /// <summary>
    /// 通过参数忽略的字段 - Field ignored via parameter
    /// </summary>
    public string Igrone { get; set; } = string.Empty;

    /// <summary>
    /// 通过特性忽略的字段 - Field ignored via attribute
    /// </summary>
    [AutoDtoIgroned]
    public string Igrone2 { get; set; } = null!;
}

/// <summary>
/// 地址信息 - Address Information
/// </summary>
public class Address
{
    [Required]
    [Display(Name = "街道")]
    public string Street { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "城市")]
    public string City { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "省份")]
    public string State { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "邮编")]
    public string ZipCode { get; set; } = string.Empty;
}

/// <summary>
/// 爱好信息 - Hobby Information
/// </summary>
public class Hobby
{
    [Required]
    [Display(Name = "爱好名称")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "爱好描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 多层嵌套示例 - Multi-level nesting example
    /// </summary>
    public HobbyExtend Extend { get; set; } = new HobbyExtend();
}

/// <summary>
/// 爱好扩展信息 - Hobby Extension Information
/// </summary>
public class HobbyExtend
{
    public string Extend1 { get; set; } = string.Empty;
    public string Extend2 { get; set; } = string.Empty;
    
    /// <summary>
    /// 更深层嵌套 - Deeper nesting level
    /// </summary>
    public InnerExtend Extend3 { get; set; } = new InnerExtend();
}

/// <summary>
/// 内部扩展信息 - Inner Extension Information
/// </summary>
public class InnerExtend
{
    public string InnerExtendMsg { get; set; } = string.Empty;
}
```

---

## 🎯 DTO 定义与标记 (Define DTO Classes)

### 🔹 基础 DTO (Basic DTO)

```csharp
using Biwen.AutoClassGen.TestConsole.Entitys;

namespace Biwen.AutoClassGen.TestConsole.Dtos
{
    /// <summary>
    /// 用户 DTO - 忽略 ID 和 TestCol 属性
    /// User DTO - Ignoring ID and TestCol properties
    /// </summary>
    [AutoDto(typeof(User), nameof(User.Id), "TestCol")]
    public partial class UserDto
    {
        // 属性将自动生成 - Properties will be auto-generated
    }

    /// <summary>
    /// C# 11+ 泛型特性支持 - Generic Attribute Support
    /// </summary>
    [AutoDto<User>(nameof(User.Email), "TestCol")]
    public partial class User3Dto
    {
        // 自动生成属性，忽略 Email 和 TestCol
        // Auto-generated properties, ignoring Email and TestCol
    }

    /// <summary>
    /// 基础人员 DTO - 只处理一层属性映射
    /// Basic Person DTO - Single level property mapping only
    /// </summary>
    [AutoDto<Person>(nameof(Person.Igrone))]
    public partial record PersonDto;
}
```

### 🔸 复杂 DTO (Complex DTO)

```csharp
/// <summary>
/// 🚀 复杂人员 DTO - 支持多层嵌套属性映射
/// Complex Person DTO - Multi-level nested property mapping support
/// 
/// ✅ AutoDto 和 AutoDtoComplex 可以同时使用
/// ✅ AutoDto and AutoDtoComplex can be used together
/// </summary>
[AutoDto<Person>(nameof(Person.Igrone))]
[AutoDtoComplex(3)] // 🔧 指定最大嵌套层级为3层，默认为2层
public partial record PersonComplexDto;

/// <summary>
/// 🌐 跨库 DTO 生成示例 - Cross-Library DTO Generation Example
/// </summary>
[AutoDto<TestClass1>("hello")] 
[AutoDtoComplex] // 🔧 使用默认的2层嵌套 - Use default 2-level nesting
public partial class LibDto
{
}
```

---

## ⭐ AutoDtoComplex 特性说明 (Features)

### 🎯 核心功能 (Core Features)

| 功能 Feature | 说明 Description |
|-------------|------------------|
| 🔗 **嵌套对象映射** | 自动生成嵌套复杂对象的 DTO 类 |
| 📚 **集合支持** | 支持 `List<T>`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>` 和数组 |
| 🎛️ **可配置嵌套层级** | 控制嵌套对象生成的最大深度 |
| 🔄 **双向映射** | 创建双向扩展方法 (Entity ↔ DTO) |
| 🌐 **跨库支持** | 支持从外部库的实体类生成 DTO |

### 🔧 参数配置 (Parameter Configuration)

```csharp
[AutoDtoComplex]             // 🔹 默认最大嵌套层级 = 2
[AutoDtoComplex(1)]          // 🔹 最大嵌套层级 = 1 (等同于基础DTO)
[AutoDtoComplex(3)]          // 🔹 最大嵌套层级 = 3 ⭐ 推荐
[AutoDtoComplex(5)]          // 🔹 最大嵌套层级 = 5 ⚠️ 谨慎使用
```

> 💡 **建议**: 层级2-3适用于大多数场景，层级4+请谨慎使用以避免性能问题

---

## 🎉 生成代码示例 (Generated Code Examples)

### 📄 基础 DTO 生成代码 (Basic DTO Generated Code)

<details>
<summary>🔍 点击查看生成的代码 (Click to view generated code)</summary>

```csharp
// <auto-generated />
// 🤖 此文件由 Biwen.AutoClassGen 自动生成
// 🤖 This file is auto-generated by Biwen.AutoClassGen
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Biwen.AutoClassGen.TestConsole.Entitys;

#pragma warning disable
namespace Biwen.AutoClassGen.TestConsole.Dtos
{
    public partial class UserDto
    {
        /// <inheritdoc cref = "User.FirstName"/>
        public string FirstName { get; set; }
        /// <inheritdoc cref = "User.LastName"/>
        public string LastName { get; set; }
        /// <inheritdoc cref = "User.Age"/>
        public int? Age { get; set; }
        /// <inheritdoc cref = "User.FullName"/>
        public string? FullName { get; set; }
        /// <inheritdoc cref = "Info.Email"/>
        public string? Email { get; set; }
        /// <inheritdoc cref = "Other.Remark"/>
        public string? Remark { get; set; }
    }
}

namespace Biwen.AutoClassGen.TestConsole.Entitys
{
    using Biwen.AutoClassGen.TestConsole.Dtos;
    
    /// <summary>
    /// 🔄 UserDto 映射扩展方法 - UserDto Mapping Extensions
    /// </summary>
    public static partial class UserToUserDtoExtentions
    {
        /// <summary>
        /// 映射到 UserDto - Map to UserDto
        /// </summary>
        /// <returns>UserDto 实例 - UserDto instance</returns>
        public static UserDto MapperToUserDto(this User model)
        {
            if (model == null) return null;
            
            return new UserDto()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Age = model.Age,
                FullName = model.FullName,
                Email = model.Email,
                Remark = model.Remark,
            };
        }
    }
}
#pragma warning restore
```

</details>

### 🏗️ 复杂 DTO 生成代码 (Complex DTO Generated Code)

复杂 DTO 将生成以下内容：

| 生成内容 Generated Content | 说明 Description |
|---------------------------|------------------|
| 🏠 **主 DTO** | PersonComplexDto 包含嵌套 DTO 属性 |
| 🧩 **嵌套 DTO** | AddressDto, HobbyDto, HobbyExtendDto, InnerExtendDto |
| 🔄 **映射扩展** | 所有 DTO 的双向映射方法 |
| 📚 **集合映射** | `List<T>` 属性的特殊处理 |

<details>
<summary>🔍 点击查看复杂 DTO 生成示例 (Click to view complex DTO example)</summary>

```csharp
// 🏠 主 DTO 类 - Main DTO Class
public partial record PersonComplexDto
{
    [Display(Name = "姓名")]
    public string Name { get; set; }
    
    [Range(0, 200)]
    [Display(Name = "年龄")]
    public int Age { get; set; }
    
    /// <summary>
    /// 🔗 嵌套 DTO - Nested DTO
    /// </summary>
    public AddressDto Address { get; set; }
    
    /// <summary>
    /// 📚 集合 DTO - Collection DTO
    /// </summary>
    public List<HobbyDto> Hobbies { get; set; }
}

// 🧩 嵌套 DTO 类 - Nested DTO Classes
public partial record AddressDto
{
    [Required, Display(Name = "街道")]
    public string Street { get; set; }
    [Required, Display(Name = "城市")]
    public string City { get; set; }
    [Required, Display(Name = "省份")]
    public string State { get; set; }
    [Required, Display(Name = "邮编")]
    public string ZipCode { get; set; }
}

public partial record HobbyDto
{
    [Required, Display(Name = "爱好名称")]
    public string Name { get; set; }
    [Required, Display(Name = "爱好描述")]
    public string Description { get; set; }
    
    /// <summary>
    /// 🔗 更深层嵌套 - Deeper nesting
    /// </summary>
    public HobbyExtendDto Extend { get; set; }
}

// 🔄 生成的映射扩展方法 - Generated Mapping Extensions
public static PersonComplexDto MapperToPersonComplexDto(this Person model)
{
    if (model == null) return null;
    
    return new PersonComplexDto()
    {
        Name = model.Name,
        Age = model.Age,
        Address = model.Address?.MapperToAddressDto(), // 🔗 嵌套对象映射
        Hobbies = model.Hobbies?.Select(x => x?.MapperToHobbyDto()).ToList(), // 📚 集合映射
    };
}
```

</details>

---

## 💻 使用示例 (Usage Examples)

### 🚀 快速开始 (Quick Start)

```csharp
// 🏗️ 创建复杂实体对象 - Create complex entity object
var person = new Person
{
    Name = "张三", // Zhang San
    Age = 25,
    Address = new Address
    {
        City = "深圳",     // Shenzhen
        State = "广东",    // Guangdong
        Street = "南山区", // Nanshan District
        ZipCode = "518000",
    },
    Hobbies = new List<Hobby>
    {
        new Hobby
        {
            Name = "编程",           // Programming
            Description = "喜欢写代码", // Love coding
            Extend = new HobbyExtend
            {
                Extend1 = "扩展信息1",  // Extension info 1
                Extend2 = "扩展信息2",  // Extension info 2
                Extend3 = new InnerExtend
                {
                    InnerExtendMsg = "深层嵌套信息" // Deep nesting info
                }
            }
        }
    }
};

// 🔹 基础 DTO 映射 (只映射一层属性)
// Basic DTO mapping (single level properties only)
var personDto = person.MapperToPersonDto();
// ⚠️ personDto.Address 是 Address 类型，不是 AddressDto
// ⚠️ personDto.Address is Address type, not AddressDto

// 🔸 复杂 DTO 映射 (支持多层嵌套)
// Complex DTO mapping (multi-level nesting support)
var personComplexDto = person.MapperToPersonComplexDto();
// ✅ personComplexDto.Address 是 AddressDto 类型
// ✅ personComplexDto.Address is AddressDto type
// ✅ personComplexDto.Hobbies 是 List<HobbyDto> 类型
// ✅ personComplexDto.Hobbies is List<HobbyDto> type

// 🔍 支持深层访问 - Deep access support
var deepInfo = personComplexDto.Hobbies[0].Extend.Extend3.InnerExtendMsg;

// 🔄 反向映射 (DTO → Entity)
// Reverse mapping (DTO → Entity)
var backToEntity = personComplexDto.MapperToPerson();
```

### 📊 性能对比 (Performance Comparison)

```csharp
// ⚡ 基础 DTO - 性能优先 (Performance first)
var basicDto = person.MapperToPersonDto();         // 快速 Fast
var json1 = JsonSerializer.Serialize(basicDto);    // 小体积 Small size

// 🔄 复杂 DTO - 功能完整 (Feature complete)  
var complexDto = person.MapperToPersonComplexDto(); // 功能全面 Full-featured
var json2 = JsonSerializer.Serialize(complexDto);   // 完整数据 Complete data
```

---

## 📚 最佳实践 (Best Practices)

### 🎯 选择合适的嵌套层级 (Choose Appropriate Nesting Level)

| 层级 Level | 使用场景 Use Case | 性能 Performance |
|-----------|------------------|------------------|
| **Level 1** | 🔹 基础 DTO，简单数据传输 | ⚡⚡⚡ 最快 |
| **Level 2-3** | 🔸 常见业务场景 ⭐ 推荐 | ⚡⚡ 良好 |
| **Level 4+** | 🔺 复杂业务对象，谨慎使用 | ⚡ 一般 |

### 🔧 属性忽略策略 (Property Ignore Strategies)

```csharp
// 方式1: 通过特性参数 - Via attribute parameters
[AutoDto<Entity>("PropertyName1", "PropertyName2")]
public partial class MyDto { }

// 方式2: 通过属性特性 - Via property attributes
public class Entity
{
    public string NormalProperty { get; set; }
    
    [AutoDtoIgroned] // 🚫 此属性将被忽略
    public string IgnoredProperty { get; set; }
}
```

### 📝 命名约定 (Naming Conventions)

```csharp
// ✅ 正确写法 - Correct usage
[AutoDto<User>]
public partial class UserDto      // DTO 类必须是 partial
{
}

// ❌ 错误写法 - Incorrect usage  
[AutoDto<User>]
public class UserDto             // 缺少 partial 关键字
{
}
```

---

## 🚨 诊断代码 (Diagnostic Codes)

| 代码 Code | 严重程度 | 描述 Description |
|-----------|---------|------------------|
| **GEN041** | ⚠️ 错误 | 重复标注 [AutoDto] (AutoDto和AutoDtoComplex可以并存，但不能有多个相同的特性) |
| **GEN042** | ⚠️ 错误 | 不能在抽象类上标记 [AutoDto] |
| **GEN044** | ⚠️ 警告 | 无法解析目标类型，请确保引用了正确的程序集且类型可访问 |
| **GEN045** | ⚠️ 警告 | 标记 [AutoDto] 的类必须声明为 partial |

---

## ⚡ 性能说明 (Performance Notes)

### 📊 性能对比表 (Performance Comparison)

| 指标 Metric | 基础 DTO | 复杂 DTO |
|------------|----------|----------|
| **编译时间** | ⚡ 快 | 🔄 较慢 |
| **内存使用** | 📦 小 | 📚 较大 |
| **功能完整性** | 🔹 基础 | 🔸 完整 |
| **推荐场景** | API 响应 | 业务逻辑 |

### 🎯 使用建议 (Usage Recommendations)

- 🔹 **API 层**: 使用基础 DTO，快速响应
- 🔸 **业务层**: 使用复杂 DTO，完整数据操作
- 🔄 **数据传输**: 根据数据复杂度选择合适模式
- ⚠️ **性能敏感**: 避免过深的嵌套层级

---

## 🌐 跨库支持 (Cross-Library Support)

> 📅 **版本支持**: 从 v2025.09.03 开始支持

```csharp
// 🌐 从外部库的类型生成 DTO
// Generate DTO from external library types
[AutoDto<ExternalLibrary.SomeClass>]
[AutoDtoComplex]
public partial class ExternalDto
{
    // 支持跨程序集的 DTO 生成
    // Support cross-assembly DTO generation
}

// 📚 支持的外部库类型
// Supported external library types
[AutoDto<ThirdParty.Models.Product>]
[AutoDtoComplex(2)]
public partial class ProductDto { }
```

---

## 🔗 相关链接 (Related Links)

- 📖 [完整文档 Full Documentation](https://github.com/vipwan/Biwen.AutoClassGen)
- 🐛 [问题反馈 Issue Reports](https://github.com/vipwan/Biwen.AutoClassGen/issues)
- 💡 [功能建议 Feature Requests](https://github.com/vipwan/Biwen.AutoClassGen/discussions)
- 📦 [NuGet Package](https://www.nuget.org/packages/Biwen.AutoClassGen)

---

## ⚠️ 注意事项 (Important Notes)

> 💡 **映射限制**: MapperTo扩展函数仅针对属性相同的情况，如果需要映射不同的属性名或特殊映射规则，请使用 `AutoMapper` 或 `Mapster` 等第三方库。

> 🔧 **部分类要求**: 所有标记了 `[AutoDto]` 的类都必须声明为 `partial`。

> 🌐 **跨库支持**: 确保外部库的类型具有足够的访问权限（public 或 internal）。

---

<div align="center">

**🎉 享受 AutoDto 带来的高效开发体验！**  
**Enjoy the efficient development experience with AutoDto!**

[![⭐ Star on GitHub](https://img.shields.io/github/stars/vipwan/Biwen.AutoClassGen?style=social)](https://github.com/vipwan/Biwen.AutoClassGen)

</div>