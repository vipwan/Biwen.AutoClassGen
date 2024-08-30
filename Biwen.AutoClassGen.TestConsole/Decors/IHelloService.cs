namespace Biwen.AutoClassGen.TestConsole.Decors
{

    //[AutoDecor(typeof(HelloServiceDecor1))]
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

    public interface IHelloServiceFor
    {
        string SayHello(string name);
    }

    [AutoInject<IHelloServiceFor>]
    public class HelloServiceFor : IHelloServiceFor
    {
        public string SayHello(string name)
        {
            return $"Hello {name} from HelloServiceFor";
        }
    }

    /// <summary>
    /// 请注意,由于HelloServiceForDecor2也标注了AutoDecorFor,
    /// 所以尽管HelloServiceForDecor也有AutoDecorFor标注,但是会被HelloServiceForDecor2覆盖!
    /// 以最后一个为准
    /// </summary>
    [AutoDecorFor(typeof(IHelloServiceFor))]
    public class HelloServiceForDecor : IHelloServiceFor
    {
        private readonly IHelloServiceFor _helloService;
        private readonly ILogger<HelloServiceForDecor> _logger;
        public HelloServiceForDecor(IHelloServiceFor helloService, ILogger<HelloServiceForDecor> logger)
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

    [AutoDecorFor<IHelloServiceFor>]
    public class HelloServiceForDecor2 : IHelloServiceFor
    {
        private readonly IHelloServiceFor _helloService;
        private readonly ILogger<HelloServiceForDecor2> _logger;
        public HelloServiceForDecor2(IHelloServiceFor helloService, ILogger<HelloServiceForDecor2> logger)
        {
            _helloService = helloService;
            _logger = logger;
        }
        public string SayHello(string name)
        {
            Console.WriteLine($"Hello {name} from HelloServiceFor2");
            var result = _helloService.SayHello(name);
            _logger.LogInformation("Hello {result} from HelloServiceFor2", result);
            return result;
        }
    }


    #endregion

}