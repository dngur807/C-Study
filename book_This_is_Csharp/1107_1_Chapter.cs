using System;// Systme 네임스페이스 안에 있는 클래스를 사용하겠다고 컴파일러에게 알리는 역할을 합니다.
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// 네임스페이스는 성격이나 하는 일이 비슷한 클래스, 구조체, 인터페이스
// 대리자 열거 형식등을 하나의 이름 아래 묶는 일을 합니다.
namespace Study 
{
    class Program
    {
        // 프로그램의 진입점 (Entry Point)로
        // 프로그램을 시작하면 실행되고, 메소드가 종료되면 프로그램도 역시 종료됩니다.
        // static : 한정자(modifier)로서 메소드나 변수 등을 수식합니다. C# 프로그램의 각 요소는
        // 코드가 실행되는 시점에 비로소 메모리에 할당되는 반면 , static 키워드로 수식되는 코드는 
        // 프로그램이 처음 구동될 때부터 진작에 메모리에 할당된다는 특징이 있습니다.

        // 프로그램이 실행되면 CLR (Common Language Runtime) 은 프로그램을 메모리에 올린 후 프로그램의 진입점을 찾는데,
        // 이 때 Main() 메소드가 static 키워드로 수식되어 있지 않다면 CLR은 진입점을 찾지 못했다는 에러 메시지를 남기고 프로그램을 종료시킬 겁니다.

        // CLR에 대해서
        // C#으로 만든 프로그램은 CLR 위에서 실행됩니다.
        // C# 컴파일러는 C# 소스 코드를 IL(Intermediate Language)라는 중간 언어로 작성된 실행 파일을 만들어 냅니다.
        // 사용자가 이 파일을 실행시키면 CLR이 중간 코드를 읽어 들여 다시 하드웨어가 이해할 수 있는 네이티브 코드로 컴파일 한 후 실행시킵니다.
        // 이것을 JIT(Just In Time) 컴파일이라 하는데, 실행에 필요한 코드를 실행할 떄마다 실시간으로 컴파일 해서 실행 한다는 뜻이다.
        // 왜 두 번씩이나 컴파일 하지?? CLR는 C# 뿐만 아니라 다른 언어도 지원하도록 설계되었습니다. 서로 다른 언어들이 만나기 위한 지점이 IL 이라는 중간 언어이고,
        // 이 언얼로 쓰인 코드를 CLR이 다시 자신이 설치되어 있는 플랫폼에 최적화 시켜 컴파일 한 후 실행 하는 것이다.
        // 이 방식의 장점은 플랫폼에 최적화된 코드를 만들어 낸다는 것이다.
        // 단점은 실행 시 이루어지는 컴파일에 대한 부담
        

        // CLR 는 단순히 언어를 동작하는 것 뿐만 아니라 예외가 발생했을 떄 이를 처리하도록 도와주는 기능, 상속 지원 , COM과 상호 운영성 지원 , 자동 메모리 관리 기능 제공
        // 이 중 자동 메모리 관리는 어려운 말로 가비지 컬렉션이라고 하는데, 프로그램에서 더 이상 사용하지 않는 메모리를 쓰레기로 간주하고 수거하는 기능을 말합니다. 
        static void Main(string[] args)
        {
            System.Console.WriteLine("test");
            Console.WriteLine("사용법 : HelloWorld.exe <이름>");
            Console.WriteLine("Hello, {0}!" , args[0]);
        }
    }
}

namespace BrainCharp
{
    // 클래스는 데이터와 데이터를 처리하는 기능으로 이루어 집니다.
    class HelloWorld
    {

    }
}
