namespace Biwen.AutoClassGen;

internal class DiagnosticDescriptors
{

    private const string Helplink = "https://github.com/vipwan/Biwen.AutoClassGen#gen-error-code";

    public const string GEN001 = "GEN001";
    public const string GEN011 = "GEN011";
    public const string GEN021 = "GEN021";
    public const string GEN031 = "GEN031"; // 推荐生成
    public const string GEN043 = "GEN043"; // 标记[AutoDecor]的类型,装饰器必须是它的实现类或者子类

    /// <summary>
    /// 无法生成类的错误
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidDeclareError = new(id: GEN001,
                                                                          title: "标注接口没有继承基础接口因此不能生成类",
                                                                          messageFormat: "没有实现基础接口因此不能生成类,请删除标注的特性[AutoGen] or 继承相应的接口",
                                                                          category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                          DiagnosticSeverity.Error,
                                                                          helpLinkUri: Helplink,
                                                                          isEnabledByDefault: true);


    /// <summary>
    /// 重名错误
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidDeclareNameError = new(id: GEN011,
                                                                          title: "生成类的类名称不可和接口名重名",
                                                                          messageFormat: "生成类的类名称不可和接口名重名",
                                                                          category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                          DiagnosticSeverity.Error,
                                                                          helpLinkUri: Helplink,
                                                                          isEnabledByDefault: true);

    /// <summary>
    /// 命名空间规范警告
    /// </summary>
    public static readonly DiagnosticDescriptor SuggestDeclareNameWarning = new(id: GEN021,
                                                                          title: "推荐使用相同的命名空间",
                                                                          messageFormat: "推荐使用相同的命名空间",
                                                                          category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                          DiagnosticSeverity.Warning,
                                                                          helpLinkUri: Helplink,
                                                                          isEnabledByDefault: true);

    /// <summary>
    /// 推荐使用自动生成
    /// </summary>
    public static readonly DiagnosticDescriptor SuggestAutoGen = new(id: GEN031,
                                                                          title: "使用[AutoGen]自动生成",
                                                                          messageFormat: "使用[AutoGen]自动生成",
                                                                          category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                          DiagnosticSeverity.Info,
                                                                          helpLinkUri: Helplink,
                                                                          isEnabledByDefault: true);

    /// <summary>
    /// Decor错误标注
    /// </summary>
    public static readonly DiagnosticDescriptor MarkedAutoDecorError = new(id: GEN043,
                                                                          title: "标记[AutoDecor]的类型,装饰器必须是它的实现类或者子类",
                                                                          messageFormat: "标记[AutoDecor]的类型,装饰器必须是它的实现类或者子类",
                                                                          category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                          DiagnosticSeverity.Error,
                                                                          helpLinkUri: Helplink,
                                                                          isEnabledByDefault: true);

}
