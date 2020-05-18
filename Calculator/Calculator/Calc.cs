using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Calculator
{
    class Calc
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Calc v 1.0");
            PrintHelp();
            StartCalculator();
        }

        private static void StartCalculator()
        {
            // Имя для pipe выбирается уникальным, чтобы можно было запускать несколько инстансов приложения одновременно.
            DateTime now = DateTime.Now;
            string pipename = "calcpipe" + now.ToString("hh_mm_ss_ffff");
            using (var namedPipeStream = new NamedPipeServerStream(pipename, PipeDirection.InOut,
                1, PipeTransmissionMode.Message))
            {
                var anotherProcess = new Process
                {
                    StartInfo =
                    {
                        FileName = "CalculatorBack/CalculatorBack.exe",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        Arguments = pipename
                    }
                };
                anotherProcess.Start();
                namedPipeStream.WaitForConnection();

                // Строка используется для сохранения и использования последнего корректного не бесконечного результата.
                string memory = "0";
                string response;

                do
                {
                    string expression = GetExpression(memory);
                    byte[] messageBytes = Encoding.Unicode.GetBytes(expression);
                    namedPipeStream.Write(messageBytes, 0, messageBytes.Length);
                    response = ProcessReceivedResult(namedPipeStream);
                    // Если полученный результат парсится в число - значит всё ок, выводим.
                    // Если не парсится - значит как минимум есть warning-и, которые хотим показать, поэтому выводим обработанное выражение пользователя.
                    // Отрицательный резултат в память записывается в скобках, так как иначе при замене ME на содержимое может получится некорректная последовательность.
                    if (Double.TryParse(response, out _))
                    {
                        if (response[0] == '-')
                            memory = String.Concat("(", response, ")");
                        else
                            memory = response;
                        Console.WriteLine("Result: {0}", response);
                    }
                    else
                    {
                        Console.WriteLine(response);
                    }
                } while (!response.Equals("Good buy") && !response.Equals("Something went wrong, sorry!"));

                anotherProcess.WaitForExit();
                anotherProcess.Close();
                Console.ReadLine();
            }
        }

        private static string ProcessReceivedResult(NamedPipeServerStream namedPipeServer)
        {
            var messageBuilder = new StringBuilder();

            do
            {
                byte[] messageBuffer = new byte[6];
                namedPipeServer.Read(messageBuffer, 0, messageBuffer.Length);
                messageBuilder.Append(Encoding.Unicode.GetString(messageBuffer));
            }
            while (!namedPipeServer.IsMessageComplete);

            return messageBuilder.ToString().Trim('\0', ' ');
        }

        private static string GetExpression(string memory)
        {
            bool flag = true;
            string result = String.Empty;

            while (flag)
            {
                Console.Write("Your expression: ");
                result = Console.ReadLine();
                if (result.Equals("help"))
                    PrintHelp();
                else
                    if (result.Length > 0)
                    flag = false;
                else
                    Console.WriteLine("Not empty expression needed");
            }

            result = result.Replace("me", memory);
            return result;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Supported only +, -, *, / operations. '-' can be used as unary.");
            Console.WriteLine("For floating point you can either use , or .");
            Console.WriteLine("All type of brakets (,{,[ will be threated as same");
            Console.WriteLine("All spaces will be ignored, but you can feel free to use them");
            Console.WriteLine("Sequnces like number(...), (...)number and (...)(...) will be treated as a multiplication");
            Console.WriteLine("Type 'me' in your expression to use recent non-infinite result in your expression");
            Console.WriteLine("Type 'help', to see this again.");
        }
    }
}
