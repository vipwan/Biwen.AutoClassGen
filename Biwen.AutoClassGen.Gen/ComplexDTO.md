# AutoClassGen 复杂对象 DTO 生成指南

本文档介绍如何使用 AutoClassGen 处理复杂对象的 DTO 生成，包括嵌套对象和集合属性的处理。

## 基本概念

在处理复杂对象时，有两种 DTO 生成模式：
1. 普通 DTO 生成：只处理一层属性映射
2. 复杂 DTO 生成：支持多层嵌套属性映射

## 实体定义示例

```csharp
// 主实体
public class Person
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required, Range(0, 200)]
    public int Age { get; set; }

    // 嵌套对象
    public Address Address { get; set; } = new();

    // 集合属性
    public List<Hobby> Hobbies { get; set; } = [];

    // 使用特性标记忽略的属性
    [AutoDtoIgroned]
    public string Igrone2 { get; set; } = null!;
}

// 嵌套实体
public class Address
{
    [Required]
    public string Street { get; set; } = string.Empty;
    [Required]
    public string City { get; set; } = string.Empty;
    [Required]
    public string State { get; set; } = string.Empty;
    [Required]
    public string ZipCode { get; set; } = string.Empty;
}

// 集合项实体
public class Hobby
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;

    // 多层嵌套
    public HobbyExtend Extend { get; set; } = new();
}

public class HobbyExtend
{
    public string Extend1 { get; set; } = string.Empty;
    public string Extend2 { get; set; } = string.Empty;
    public InnerExtend Extend3 { get; set; } = new();
}

public class InnerExtend
{
    public string InnerExtendMsg { get; set; } = string.Empty;
}
```

## DTO 定义

### 1. 普通 DTO（单层映射）

```csharp
/// <summary>
/// 没有复杂属性嵌套的 DTO
/// </summary>
[AutoDto<Person>(nameof(Person.Igrone))]
public partial record PersonDto;
```

特点：
- 使用 `partial record` 定义
- 使用 `AutoDto<T>` 特性指定源类型
- 可以通过特性参数指定要忽略的属性
- 只会生成一层属性的映射

### 2. 复杂 DTO（多层嵌套）

```csharp
/// <summary>
/// 模拟的复杂 DTO
/// </summary>
[AutoDto<Person>(nameof(Person.Igrone))]
[AutoDtoComplex(3)]
public partial record PersonComplexDto;
```

特点：
- 使用 `partial record` 定义
- 必须添加 `[AutoDtoComplex]` 特性
- 可以通过 `AutoDtoComplex` 的参数指定最大嵌套层级（默认为 2）
- 支持深层嵌套属性的映射

## 使用示例

```csharp
// 创建实体对象
var person = new Person
{
    Name = "测试",
    Age = 18,
    Address = new Address
    {
        City = "深圳",
        State = "广东",
        Street = "某街道",
        ZipCode = "518000",
    },
    Hobbies = 
    [
        new Hobby
        {
            Name = "篮球",
            Description = "喜欢打篮球",
            Extend = new HobbyExtend
            {
                Extend1 = "扩展1",
                Extend2 = "扩展2",
                Extend3 = new InnerExtend
                {
                    InnerExtendMsg = "内部扩展信息"
                }
            }
        }
    ]
};

// 生成普通 DTO（单层映射）
var personDto = person.MapperToPersonDto();

// 生成复杂 DTO（多层嵌套）
var personComplexDto = person.MapperToPersonComplexDto();
```

## 注意事项

1. **命名约定**：
   - DTO 类必须声明为 `partial record`
   - 生成的映射方法命名规则为 `MapperTo{DTO名称}`

2. **忽略属性**：
   - 通过特性参数指定：`[AutoDto<T>("PropertyName")]`
   - 通过属性特性标记：`[AutoDtoIgroned]`

3. **嵌套层级**：
   - 普通 DTO 只处理一层映射
   - 使用 `[AutoDtoComplex(n)]` 可以指定最大嵌套层级
   - 推荐根据实际需求设置合适的嵌套层级，避免过深的嵌套

4. **性能考虑**：
   - 普通 DTO 适用于简单的数据传输场景
   - 复杂 DTO 适用于需要完整数据结构的场景
   - 嵌套层级越深，性能开销越大

5. **类型映射**：
   - 基础类型和集合类型会自动处理
   - 支持 `List<T>` 等常用集合类型
   - 保留数据验证特性（如 `[Required]`）