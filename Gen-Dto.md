# 🚀 AutoDto Generator - DTO自动生成器

[![Nuget](https://img.shields.io/nuget/v/Biwen.AutoClassGen)](https://www.nuget.org/packages/Biwen.AutoClassGen)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/LICENSE.txt)

AutoDto Generator 是一个源代码生成器，可以自动从实体类生成 DTO 对象并创建双向映射扩展方法。支持基础 DTO 和复杂嵌套 DTO 两种模式, 以及可选的静态映射器扩展 (AutoDtoWithMapper)。

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

// 带静态映射器 (仅 .NET 7+/C#11+, 需要 IStaticAutoDtoMapper<TFrom,TTo>)
[AutoDtoWithMapper<TFrom>(typeof(YourMapper), params string[] ignoredProperties)]
```

> AutoDtoWithMapper 是 AutoDto 的增强形式, 在生成默认属性复制后, 会自动调用你提供的静态 `Mapper` 的 `Map(TFrom from, TTo to)` 方法, 最后仍会调用局部 `partial void MapperToPartial(...)` 钩子, 执行顺序: 默认属性复制 → 静态 Mapper → partial 钩子。

### 🧩 静态映射器接口 (Static Mapper Interface)

```csharp
// 仅在 NET7_0_OR_GREATER 条件下可用
public interface IStaticAutoDtoMapper<TFrom, TTo>
{
    static abstract void Map(TFrom from, TTo to);
}
```

### 🛠️ AutoDtoWithMapper 使用示例 (Usage Example)

```csharp
// 源实体
public class User
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public string? Email { get; set; }
}

// DTO (生成器会自动生成属性, 这里只留空 partial 声明)
[AutoDtoWithMapper<User>(typeof(UserStaticMapper), nameof(User.Email))]
public partial class UserWithMapperDto { }

// 静态映射器 (可选: 实现 IStaticAutoDtoMapper<User, UserWithMapperDto>)
public class UserStaticMapper : IStaticAutoDtoMapper<User, UserWithMapperDto>
{
    public static void Map(User from, UserWithMapperDto to)
    {
        // 自定义补充逻辑 (覆盖或增强默认赋值)
        to.FirstName = from.FirstName + " (STATIC)";
    }
}
```

生成的 `MapperToUserWithMapperDto` 将包含:
1. 基础属性复制 (除忽略字段)  
2. `UserStaticMapper.Map(model, retn);` 调用  
3. `MapperToPartial(model, retn);`  (可选再扩展)

> 如果提供的 mapper 没有正确实现接口或泛型不匹配, 生成器会忽略静态调用并给出诊断 (GEN046 / GEN047)。

### ⚖️ AutoDto vs AutoDtoWithMapper 对比

| 能力 | AutoDto | AutoDtoWithMapper |
|------|---------|------------------|
| 基础属性复制 | ✅ | ✅ |
| 嵌套/复杂支持 (配合 AutoDtoComplex) | ✅ | ✅ |
| 自定义后处理 partial 钩子 | ✅ | ✅ |
| 统一/可复用集中映射逻辑 | ❌ 需要每处写 partial | ✅ 通过静态 mapper 复用 |
| 泛型接口契约约束 | ❌ | ✅ (IStaticAutoDtoMapper) |

---

## 📊 DTO 模式对比 (Basic DTO vs Complex DTO)

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
    public class User : Info
    {
        public string Id { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public int? Age { get; set; }
        public string? FullName => $"{FirstName} {LastName}";
        public string? Email { get; set; }
    }
    public abstract class Info : Other {}
    public abstract class Other { public string? Remark { get; set; } }
}
```

### 🔗 复杂实体示例 (Complex Entity Example)

```csharp
public class Person { /* 略, 与原文相同 */ }
public class Address { /* 略 */ }
public class Hobby { /* 略 */ }
public class HobbyExtend { /* 略 */ }
public class InnerExtend { /* 略 */ }
```

---

## 🎯 DTO 定义与标记 (Define DTO Classes)

(内容同前, 略)

---

## 🧠 AutoDtoWithMapper 诊断说明 (Diagnostics)

| 代码 | 严重程度 | 说明 |
|------|----------|------|
| GEN041 | Error | 重复标注 [AutoDto]/[AutoDtoComplex] |
| GEN042 | Error | 抽象类不能标注 [AutoDto] |
| GEN044 | Warning | 无法解析目标实体类型 |
| GEN045 | Warning | DTO 类必须是 partial |
| GEN046 | Warning | [AutoDtoWithMapper] 缺少或非法 mapper 参数 (null / 非 typeof) |
| GEN047 | Warning | mapper 未正确实现 IStaticAutoDtoMapper<TFrom,TTo> 泛型 (或泛型不匹配当前 DTO) |

> 提示: 当出现 GEN046/GEN047 时, 静态映射器调用会被跳过, 仍执行默认属性复制与 partial 钩子。

---

## 🔍 静态映射器实现注意点 (Implementation Notes)

- Mapper 可以实现 `IStaticAutoDtoMapper<TFrom,TTo>` 以获得强类型校验。  
- 也允许只提供 `public static void Map(TFrom, TTo)` 方法 (未实现接口) —— 会尝试匹配签名。  
- 仅支持正向映射静态扩展 (DTO→实体仍使用 partial 钩子)。后续可扩展支持反向静态映射。  
- 忽略属性逻辑与 AutoDto 一致: 构造参数名称 / `nameof()` / `[AutoDtoIgroned]`。

---

<div align="center">

**🎉 享受 AutoDto 带来的高效开发体验！**  
**Enjoy the efficient development experience with AutoDto!**

[![⭐ Star on GitHub](https://img.shields.io/github/stars/vipwan/Biwen.AutoClassGen?style=social)](https://github.com/vipwan/Biwen.AutoClassGen)

</div>