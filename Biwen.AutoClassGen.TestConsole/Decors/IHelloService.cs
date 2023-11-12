namespace Biwen.AutoClassGen.TestConsole.Decors
{

    [AutoDecor(typeof(HelloServiceDecor2))]
    [AutoDecor<HelloServiceDecor1>]
    public interface IHelloService
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
    public class ClassService
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
            _logger.LogInformation($"Hello {result} from ClassServiceDecor");
            return result;

        }
    }


    /// <summary>
    /// decor IHelloService
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
            var result= _helloService.SayHello(name);

            _logger.LogInformation($"Hello {result} from HelloServiceDecor1");

            return result;
        }
    }
    /// <summary>
    /// decor IHelloService 2
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
            Console.WriteLine($"Hello {name} from HelloServiceDecor2");
            var result= _helloService.SayHello(name);
            _logger.LogInformation($"Hello {result} from HelloServiceDecor2");
            return result;

        }
    }


}