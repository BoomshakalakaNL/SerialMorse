using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SerialMorse.Classes;
using Windows.Devices.Gpio;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace SerialMorse
{
    public sealed partial class MainPage : Page
    {
        private LCD lcd;
        private LED led;
        private Pushbutton btnInvoer;
        private Pushbutton btnBevestig;
        private Stopwatch stopwatch;
        private Stopwatch stopwatchTryDecode;
        private DispatcherTimer timer;

        
        private string[,] characters = new string[36, 2] { { "A", ".-" }, { "B", "-..." }, { "C", "-.-." }, { "D", "-.." }, { "E", "." },
                                                       { "F", "..-." }, { "G", "--." }, { "H", "...." }, { "I", ".." }, { "J", ".---" },
                                                       { "K", "-.-" }, { "L", ".-.." }, { "M", "--" }, { "N", "-." }, { "O", "---" },
                                                       { "P", ".--." }, { "Q", "--.-" }, { "R", ".-." }, { "S", "..." }, { "T", "-" },
                                                       { "U", "..-" }, { "V", "...-" }, { "W", ".--" }, { "X", "-..-" }, { "Y", "-.--" },
                                                       { "Z", "--.." }, { "0","-----"}, { "1", ".----" }, { "2", "..---"},{ "3", "...--"},
                                                       { "4", "....-"}, { "5", "....."}, { "6", "-...."}, { "7", "--..."}, { "8", "---.."},
                                                       { "9", "----."}};

        private int tijdPunt = 50; // in miliseconden

        private string code;
        private string bericht;
        private int knopDownTijd; // vastleggen tijd wanneer knop is ingedrukt
        private int knopDownLengte; //vastleggen tijd hoelang de knop is ingehouden
        private int knopUpTijd; //vastleggen tijd wanneer los gelaten



        public MainPage()
        {
            this.InitializeComponent();
            this.Init();
        }

        private void Init()
        {
            this.lcd = new LCD(16, 2);
            this.InitAsync();

            Task.Delay(1000).Wait();

            this.led = new LED(17, GpioPinDriveMode.Output);
            this.btnInvoer = new Pushbutton(27, GpioPinDriveMode.InputPullUp);
            this.btnBevestig = new Pushbutton(21, GpioPinDriveMode.InputPullUp);

            this.stopwatch = new Stopwatch();
            this.stopwatchTryDecode = new Stopwatch();

            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromMilliseconds(10);
            this.timer.Tick += timerTick;
            this.timer.Start();

        }

        private async void InitAsync()
        {
            await this.lcd.InitAsync(18, 23, 24, 5, 6, 13);
            await this.lcd.clearAsync();
            this.lcd.WriteLine("Chalo");
        }

        private void timerTick(object sender, object e)
        {
            if(btnInvoer.Pressed())
            {
                stopwatch.Start();
                stopwatchTryDecode.Restart();

                led.Aan();
            }
            if (!btnInvoer.Pressed())
            {
                stopwatch.Stop();
                led.Uit();
                knopDownTijd = 0;
                knopUpTijd = unchecked((int)stopwatch.ElapsedMilliseconds);
                knopDownLengte = knopUpTijd - knopDownTijd;

                if (knopDownLengte >= tijdPunt * 10 && bericht != null && bericht != "")
                {
                    bericht = bericht.Remove(bericht.Length - 1);
                    stopwatch.Reset();
                    printBericht();
                }

                else if (knopDownLengte >= tijdPunt * 3)
                {
                    code += "-";
                    stopwatch.Reset();
                    printCode();
                }
                else if (knopDownLengte > 0)
                {
                    code += ".";
                    stopwatch.Reset();
                    printCode();
                }

                if (unchecked((int)stopwatchTryDecode.ElapsedMilliseconds) >= tijdPunt * 15)
                {
                    if (code != "")
                    {
                        Debug.WriteLine("Try DECODE now");
                        ControleerCode();
                        printBericht();
                        code = "";
                        stopwatchTryDecode.Restart();
                    }

                    else if (unchecked((int)stopwatchTryDecode.ElapsedMilliseconds) >= tijdPunt * 50 && bericht != null && bericht != "")
                    {
                        char last = bericht[bericht.Length - 1];
                        if (last.ToString() != " ")
                        {
                            Debug.WriteLine("Set SPACE now");
                            bericht += " ";
                            printBericht();
                            stopwatchTryDecode.Reset();
                        }

                    }
                }
            }            
        }

        private void printBericht()
        {
            Debug.WriteLine("Bericht: "+bericht);
        }

        private void printCode()
        {
            //this.lcd.setCursor(0, 1);
            //this.lcd.write("Code: " + this.code);
            Debug.WriteLine("Code: " + this.code);
        }

        private void ControleerCode()
        {
            int match = -1;
            for (int i = 0; i < this.characters.Length/2; i++)
            {
                if (this.code == this.characters[i,1])
                {
                    match = i;
                }
            }
            if(match!= -1)
            {
                this.bericht += this.characters[match,0];
            }
            else
            {
                printInvalidCode();
            }
        }

        private void printInvalidCode()
        {
            //this.lcd.setCursor(0, 1);
            //this.lcd.write("Code invalid    ");
            Debug.WriteLine("Code invalid    ");
        }
    }
}
