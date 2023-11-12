## Biwen.AutoClassGen
使用场景:很多时候我们的请求对象会特别多比如GetIdRequest,GetUserRequest etc...,这些Request可能大量存在相同的字段,
比如多租户Id,分页数,这些属性字段可能又存在验证规则,绑定规则,以及Swagger描述等信息,
如果这些代码都需要人肉敲那会增加很多工作量,所以Biwen.AutoClassGen应运而生,解决这个痛点...
- 用于生成C#类的工具，自动生成类的属性,并且属性的Attribute全部来自Interface

### 用法

```bash
dotnet add package Biwen.AutoClassGen.Attributes
```

- [DTO生成器文档](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-Dto.md)
- [Req生成器文档](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-request.md)
- [Decor装饰器文档](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/Gen-Decor.md)

### Used by
#### if you use this library, please tell me, I will add your project here.
- [Biwen.QuickApi](https://github.com/vipwan/Biwen.QuickApi)
