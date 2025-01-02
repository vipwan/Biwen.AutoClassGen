# Biwen.AutoClassGen

![Nuget](https://img.shields.io/nuget/v/Biwen.AutoClassGen)
![Nuget](https://img.shields.io/nuget/dt/Biwen.AutoClassGen)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/LICENSE.txt) 
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/vipwan/Biwen.AutoClassGen/pulls) 

#### Usage scenario

- In many cases, we will have a lot of request objects,
such as GetIdRequest, GetUserRequest, etc..., and these requests may have a large number of the same fields.
For example, the multi-tenant Id, the number of pages, and these attribute fields may have validation rules, binding rules, and Swagger descriptions.
If all this code needs to be written, it will add a lot of work, so Biwen.AutoClassGen came into being to solve this pain point...
- In many cases, we will have a lot of DTO objects,
- AOP & Decorator
- Auto Inject
- Version Info

[中文](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/README-zh.md)

### Usage

```xml
<ItemGroup>
	<PackageReference Include="Biwen.AutoClassGen.Attributes" Version="x.x.x" />
	<PackageReference Include="Biwen.AutoClassGen" Version="x.x.x" PrivateAssets="all" />
</ItemGroup>
```

### Code Generators

- [Gen DTO Usage doc](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-Dto.md)
- [Gen Request Usage doc](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-request.md)
- [Gen Decoration Usage doc](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-Decor.md)
- [Gen AutoInject Usage doc](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-AutoInject.md)
- [Gen Version](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-Version.md)
- [Gen Assembly Metadata](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-Metadata.md)

### Code Analyzers

- `GEN001` : 标注接口没有继承基础接口因此不能生成类
- `GEN011` : 生成类的类名称不可和接口名重名
- `GEN021` : 推荐使用相同的命名空间
- `GEN031` : 使用`[AutoGen]`自动生成
- `GEN041` : 重复标注`[AutoDto]`
- `GEN042` : 不可在`abstract`类上标注`[AutoDto]`
- `GEN043` : 标记为`[AutoDecor]`的类必须是`public`的
- `GEN044` : `[AutoDto]`引用了外部类是不允许的
- `GEN045` : `[AutoDto]`标注的类必须是partial类
- `GEN050` : 文件缺少头部信息
- `GEN051` : 异步方法应该以`Async`结尾
- `GEN052` : 建议使用文件范围命名空间
- `GEN053` : 源代码非`UTF-8`编码
 
### Code Fixs

- 移除无效的`[AutoDto]`标注
- 使用`[AutoGen]`自动生成
- 推荐使用相同的命名空间
- 文件缺少头部信息
- 异步方法应该以`Async`结尾
- .etc



### Used by
#### if you use this library, please tell me, I will add your project here.
- [Biwen.QuickApi](https://github.com/vipwan/Biwen.QuickApi)
