using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Diagnostics;

namespace KonstrukcijskaVjezba1
{
    class Program
    {
        // Morse.Json se uzima, čita i postavlja u listu Values koju koristimo u funkcijama za pretvorbu u Morseov kod i iz Morseovog koda.
        static Dictionary<char, string> values;
        private static void Dictionary()
        {
            string sJson = "";
            StreamReader oSr = new StreamReader("morse.json");
            using (oSr)
            {
                sJson = oSr.ReadToEnd();
            }
            values = JsonConvert.DeserializeObject<Dictionary<char, string>>(sJson);
        }
        static void Main(string[] args)
        {
            Dictionary();
            Console.Write("Enter your name: ");
            string sIme = Console.ReadLine();
            MqttClient client = new MqttClient("broker.hivemq.com");  // Spajamo se na MqttBroker(MQ Telemetry Transport), 
            byte code = client.Connect(Guid.NewGuid().ToString());    // U ovom slucaju je to javan besplatan broker 
            client.Subscribe(new string[] { "/text", "/morse" },      // Pretplatili smo se na dvije teme.
            new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE,          // Quality of service
                         MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE }); 

            Console.WriteLine("Select action: ");
            Console.Write("{0}{1}{2}", "1. Text message", "\n2. Morse message", "\n   = ");  // Odabiremo akciju
        AKCIJA:
            int iBroj = int.Parse(Console.ReadLine());
            Console.Clear();
            switch (iBroj)
            {
                case 1:
                    client.MqttMsgPublished += client_MqttMsgPublished;                    // Koristimo ovu funkciju za slanje poruka
                    client.MqttMsgPublishReceived += client_MqttMsgPublishReceived1;       // Te ovu funkciju za primanje poruka
                PORUKA:
                    System.Threading.Thread.Sleep(500);                                    // Konzola je na cekanju zbog spore brzine poruka.
                    String sMessage = TextToMorse(Console.ReadLine());                     // Upisujemo poruku te se poziva funkcija za pretvorbu
                    Console.SetCursorPosition(0, Console.CursorTop - 1);                   // poruke u Morseov kod
                    Console.WriteLine("{0}{1}{2}", sIme,": ", sMessage);
                    client.Publish("/text", Encoding.UTF8.GetBytes(sIme + ": " + sMessage), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false); 
                    goto PORUKA;                                                           // Šaljemo poruku te se vraćamo na početak
                case 2:
                    client.MqttMsgPublished += client_MqttMsgPublished2;
                    client.MqttMsgPublishReceived += client_MqttMsgPublishReceived2;
                MORSE:
                    System.Threading.Thread.Sleep(500);
                    String sMorse = Console.ReadLine(); 
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.WriteLine("{0}{1}", "You wrote: ", sMorse);
                    client.Publish("/morse", Encoding.UTF8.GetBytes(sIme + ':' + sMorse), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    goto MORSE;
                default:
                    Console.Write("Invalid action, select action again \n   =");           // Ako pritisnemo krivu tipku u izborniku, vraćamo se na
                    goto AKCIJA;                                                           // početak izbornika, te odabiremo ponovno tip poruke
            }
        }
        public static string TextToMorse(String input)          // Funkcija koja pretvara tekstualnu poruku u Morseov kod
        {
            StringBuilder sb = new StringBuilder();
            input = input.ToUpper();                            // Input pretvaramo u velika slova zato što su slova u "morse.Json" isto tako velika slova 
            for (int i = 0; i < input.Length; i++)              // for petlja se vrti sve do kad je "i" manje od broja slova u riječi ili rećenici.
            {                                                                       
                if (i > 0)                                      // Dodajemo razmak između svakog slova i rećenice
                {
                    sb.Append(" ");
                }
                char c = input[i];
                if (values.ContainsKey(c))                      // Ako lista "values", sadrži kljuć "C", npr: slovo "E", od toga dobivamo "."
                {
                    sb.Append(values[c]);
                }
            }
            return sb.ToString();
        }
        public static string MorseToText(String morseInput)     // Funkcija koja pretvara Morseov kod nazad u tekst
        {
            StringBuilder sb = new StringBuilder();
            String[] morseChars = morseInput.Split(' ');        
            for (int i = 0; i < morseChars.Length; i++)
            {
                if (i > 0)                                      // Dodajemo razmak
                    sb.Append(" ");                             
                String eng = morseChars[i];
                if (values.ContainsValue(eng))                  // Ako lista "values" sadrži vrijednost "eng", pretvori vrijednost u njegov ključ
                {
                    sb.Append(values.First(kv => kv.Value.Equals(eng, StringComparison.OrdinalIgnoreCase)).Key);
                }
            }
            return sb.ToString();
        }
        public static string morseSound(String character)       // Funkcija pretvara Morseov kod u Morseov input
        {
            StringBuilder sb = new StringBuilder();
            char[] morsearray = character.ToCharArray();        
            for (int i = 0; i < morsearray.Length; i++)
            {
                Console.Write(morsearray[i]);
                if (morsearray[i] == '-')
                    Console.Beep(1020, 500);                    
                else if (morsearray[i] == '.')
                    Console.Beep(1020, 100);
                else if (morsearray[i] == ' ')
                    System.Threading.Thread.Sleep(500);
                else
                    System.Threading.Thread.Sleep(250);
            }
            return sb.ToString();
        }
        static void client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)       // Funkcija za slanje poruka
        {
            //Console.WriteLine("/text" + e.MessageId + " Published = " + e.IsPublished);
        }
        static void client_MqttMsgPublished2(object sender, MqttMsgPublishedEventArgs e)      // Funkcija za slanje poruka
        {
            //Console.WriteLine("/text" + e.MessageId + " Published = " + e.IsPublished);
        }
        static void client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)     // Subscribe na topic
        {
            //Console.WriteLine("/text" + e.MessageId);
        }
        static void client_MqttMsgPublishReceived1(object sender, MqttMsgPublishEventArgs e)  // Funkcija koja primljeni Morseov kod pretvara u text
        {
            String name = Encoding.UTF8.GetString(e.Message).Split(':')[0];
            String message = MorseToText(Encoding.UTF8.GetString(e.Message).Split(':')[1]);
            Console.WriteLine(name + ':' + message);
            Console.WriteLine("/-----------------------------------------------------/");
        }
        static void client_MqttMsgPublishReceived2(object sender, MqttMsgPublishEventArgs e)  // Funkcija koja primljeni Morseov kod pretvara u sound
        {                                                                                     // te ga ispisuje na zaslon
            String name = Encoding.UTF8.GetString(e.Message).Split(':')[0];
            String message = Encoding.UTF8.GetString(e.Message).Split(':')[1];
            Console.Write("{0}{1}{2}", "Morse code sent by ", name ,": ");
            morseSound(message);
            Console.WriteLine("\n/-----------------------------------------------------/");
        }
    }
}

