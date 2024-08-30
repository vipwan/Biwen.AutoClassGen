namespace Biwen.AutoClassGen.TestConsole.Decors
{

    [AutoDecor(typeof(HelloServiceDecor1))]
    [AutoDecor<HelloServiceDecor2>]
    public partial interface IHelloService
    {
        string SayHello(string name);
    }

    public class HelloService : IHelloService
    {
        public string SayHello(string name)
        {
            return $"Hello {name}";
        }
    }

    /// <summary>
    /// ClassService
    /// </summary>
    [AutoDecor<ClassServiceDecor>]
    public partial class ClassService
    {
        /// <summary>
        /// 请注意，如果TService是一个类,而不是interface,这里的virtual关键字是必须的
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual string SayHello(string name)
        {
            return $"Hello {name}";
        }
    }

    /// <summary>
    /// decor for ClassService
    /// </summary>
    public class ClassServiceDecor : ClassService
    {
        private readonly ClassService _helloService;
        private readonly ILogger<ClassServiceDecor> _logger;

        public ClassServiceDecor(ClassService helloService, ILogger<ClassServiceDecor> logger)
        {
            _helloService = helloService;
            _logger = logger;
        }

        public override string SayHello(string name)
        {
            Console.WriteLine($"Hello {name} from ClassServiceDecor");
            var result = _helloService.SayHello(name);
            _logger.LogInformation("Hello {result} from ClassServiceDecor", result);
            return result;

        }
    }


    /// <summary>
    /// decor for IHelloService
    /// </summary>
    public class HelloServiceDecor1 : IHelloService
    {
        private readonly IHelloService _helloService;

        private readonly ILogger<HelloServiceDecor1> _logger;


        public HelloServiceDecor1(IHelloService helloService, ILogger<HelloServiceDecor1> logger)
        {
            _helloService = helloService;
            _logger = logger;
        }

        public string SayHello(string name)
        {
            Console.WriteLine($"Hello {name} from HelloServiceDecor1");
            var result = _helloService.SayHello(name);

            _logger.LogInformation("Hello {result} from HelloServiceDecor1", result);

            return result;
        }
    }
    /// <summary>
    /// decor for IHelloService 2
    /// </summary>
    public class HelloServiceDecor2 : IHelloService
    {
        private readonly IHelloService _helloService;
        private readonly ILogger<HelloServiceDecor2> _logger;

        public HelloServiceDecor2(IHelloService helloService, ILogger<HelloServiceDecor2> logger)
        {
            _helloService = helloService;
            _logger = logger;
        }

        public string SayHello(string name)
        {
            Console.WriteLine("Hello {0} from HelloServiceDecor2", name);
            var result = _helloService.SayHello(name);
            _logger.LogInformation("Hello {result} from HelloServiceDecor2", result);
            return result;

        }
    }



    #region Decorator for

    [AutoDecorFor(typeof(IHelloService))]
    public class HelloServiceFor : IHelloService
    {
        private readonly IHelloService _helloService;
        private readonly ILogger<HelloServiceFor> _logger;
        public HelloServiceFor(IHelloService helloService, ILogger<HelloServiceFor> logger)
        {
            _helloService = helloService;
            _logger = logger;
        }
        public string SayHello(string name)
        {
            Console.WriteLine($"Hello {name} from HelloServiceFor");
            var result = _helloService.SayHello(name);
            _logger.LogInformation("Hello {result} from HelloServiceFor", result);
            return result;
        }
    }


    /// <summary>
    /// 当前两种模式标注都支持,但是因为For相同,所以重复的标注会被忽略
    /// </summary>
    [AutoDecorFor(typeof(IHelloService))]
    [AutoDecorFor<IHelloService>]
    public class HelloServiceFor2 : IHelloService
    {
        private readonly IHelloService _helloService;
        private readonly ILogger<HelloServiceFor> _logger;
        public HelloServiceFor2(IHelloService helloService, ILogger<HelloServiceFor> logger)
        {
            _helloService = helloService;
            _logger = logger;
        }
        public string SayHello(string name)
        {
            Console.WriteLine($"Hello {name} from HelloServiceFor");
            var result = _helloService.SayHello(name);
            _logger.LogInformation("Hello {result} from HelloServiceFor", result);
            return result;
        }
    }

    #endregion




}