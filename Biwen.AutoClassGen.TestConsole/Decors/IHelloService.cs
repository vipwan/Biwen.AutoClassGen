namespace Biwen.AutoClassGen.TestConsole.Decors
{

    [AutoDecor(typeof(HelloServiceDecor1))]
    [AutoDecor<HelloServiceDecor2>]
    public interface IHelloService
    {
        string SayHello(string name);
    }

    /// <summary>
    /// implement IHelloService
    /// </summary>
    [AutoDecor<HelloServiceDecor1>]
    public class HelloService : IHelloService
    {
        public string SayHello(string name)
        {
            return $"Hello {name}";
        }
    }

    /// <summary>
    /// decor IHelloService
    /// </summary>
    public class HelloServiceDecor1 : HelloService
    {
        private readonly HelloService _helloService;

        public HelloServiceDecor1(HelloService helloService)
        {
            _helloService = helloService;
        }

        public new string SayHello(string name)
        {
            Console.WriteLine($"Hello {name} from HelloServiceDecor1");
            return _helloService.SayHello(name);
        }
    }
    /// <summary>
    /// decor IHelloService 2
    /// </summary>
    public class HelloServiceDecor2 : IHelloService
    {
        private readonly IHelloService _helloService;

        public HelloServiceDecor2(IHelloService helloService)
        {
            _helloService = helloService;
        }

        public string SayHello(string name)
        {
            Console.WriteLine($"Hello {name} from HelloServiceDecor2");
            return _helloService.SayHello(name);
        }
    }


}