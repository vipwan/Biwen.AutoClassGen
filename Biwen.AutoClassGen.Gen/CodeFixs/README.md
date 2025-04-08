# 代码修复提供程序使用说明

## 文件头部注释修复 (AddFileHeaderCodeFixProvider)

### 功能说明
自动为代码文件添加标准的头部注释信息。支持从项目配置中读取元数据，并支持变量替换。

### 配置方法

1. 在项目目录下创建 `Biwen.AutoClassGen.Comment` 文件，定制注释模板
2. 如果未创建配置文件，将使用默认模板:
```csharp
// Licensed to the {Product} under one or more agreements.
// The {Product} licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
```

### 支持的变量
- `{Product}`: 程序集名称
- `{Title}`: 项目名称
- `{Version}`: 版本号
- `{Date}`: 当前日期时间
- `{Author}`: 作者（默认为当前用户名）
- `{Company}`: 公司名称
- `{Copyright}`: 版权信息
- `{File}`: 文件名
- `{Description}`: 描述信息
- `{TargetFramework}`: 目标框架

### 使用方式
1. 当VS Code提示文件缺少头部注释时，点击快速修复选项
2. 选择"添加文件头部信息"
3. 自动插入配置的头部注释

## 异步方法名修复 (AsyncMethodNameCodeFixProvider)

### 功能说明
自动将异步方法名修改为以"Async"结尾的标准命名形式。

### 特性
- 自动处理接口实现
- 同步修改所有相关引用
- 处理继承关系中的方法重写
- 支持注释中的方法名更新
- 保留方法重载

### 使用方式
1. 当检测到异步方法名不以"Async"结尾时，`VS` & `VS Code`会显示提示
2. 点击快速修复选项
3. 选择"将异步方法名改为以Async结尾"
4. 工具会自动：
   - 重命名当前方法
   - 更新所有调用该方法的代码
   - 同步修改接口定义（如果存在）
   - 更新所有实现类中的方法名

### 最佳实践
1. 始终遵循异步方法命名约定
2. 在重构之前确保所有相关代码都已保存
3. 检查重命名后的代码确保没有遗漏
