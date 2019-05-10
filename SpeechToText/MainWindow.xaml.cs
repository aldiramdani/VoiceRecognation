#region using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Recognition;
using System.Windows;
using System.Diagnostics;
using System.Speech.Recognition.SrgsGrammar;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
#endregion

namespace SpeechToText
{
    /// <summary>
    /// MainWindow class
    /// </summary>
    public partial class MainWindow : Window
    {
        #region locals

        /// <summary>
        /// the engine
        /// </summary>
        SpeechRecognitionEngine speechRecognitionEngine = null;
        // IP Adress
        IPEndPoint ip = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);

        Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        /// <summary>
        /// list of predefined commands
        /// </summary>
        List<Word> words = new List<Word>();
        string sendKata;

        #endregion

        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            try
            {
               
                // create the engine
                speechRecognitionEngine = createSpeechEngine("en-US");

                // hook to events
                speechRecognitionEngine.AudioLevelUpdated += new EventHandler<AudioLevelUpdatedEventArgs>(engine_AudioLevelUpdated);
                speechRecognitionEngine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(engine_SpeechRecognized);

                // load dictionary
                loadGrammarAndCommands();

                // use the system's default microphone
                speechRecognitionEngine.SetInputToDefaultAudioDevice();

                // start listening
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Voice recognition failed");
            }
        }

        #endregion

        /// Socket
        public void sendSocket()
        {
            try
            {
                server.Connect(ip);
            }
            catch (SocketException e)
            {
                Console.WriteLine("Unable to connect to server.");
                return;
            }

            while (true)
            {
                string input = sendKata;
                server.Send(Encoding.ASCII.GetBytes(input));
                byte[] data = new byte[1024];
                int receivedDataLength = server.Receive(data);
                string stringData = Encoding.ASCII.GetString(data, 0, receivedDataLength);
                Console.WriteLine(stringData);
            }

            server.Shutdown(SocketShutdown.Both);
            server.Close();
        }

        #region internal functions and methods

        /// <summary>
        /// Creates the speech engine.
        /// </summary>
        /// <param name="preferredCulture">The preferred culture.</param>
        /// <returns></returns>
        private SpeechRecognitionEngine createSpeechEngine(string preferredCulture)
        {
            foreach (RecognizerInfo config in SpeechRecognitionEngine.InstalledRecognizers())
            {
                if (config.Culture.ToString() == preferredCulture)
                {
                    speechRecognitionEngine = new SpeechRecognitionEngine(config);
                    break;
                }
            }

            // if the desired culture is not found, then load default
            if (speechRecognitionEngine == null)
            {
                MessageBox.Show("The desired culture is not installed on this machine, the speech-engine will continue using "
                    + SpeechRecognitionEngine.InstalledRecognizers()[0].Culture.ToString() + " as the default culture.",
                    "Culture " + preferredCulture + " not found!");
                speechRecognitionEngine = new SpeechRecognitionEngine(SpeechRecognitionEngine.InstalledRecognizers()[0]);
            }

            return speechRecognitionEngine;
        }

        /// <summary>
        /// Loads the grammar and commands.
        /// </summary>
        private void loadGrammarAndCommands()
        {
            try
            {
                Choices texts = new Choices();

                string strResultJson = File.ReadAllText(Environment.CurrentDirectory+"\\data.json");
                var dictionary = JsonConvert.DeserializeObject<List<Word>>(strResultJson);
                for(int i = 0;i < dictionary.Count(); i++)
                {
                    words.Add(new Word() { kataUcapan = dictionary[i].kataUcapan, kata = dictionary[i].kata });
                    Console.WriteLine(dictionary[i].kataUcapan);
                    texts.Add(dictionary[i].kataUcapan);
                }


                Grammar wordsList = new Grammar(new GrammarBuilder(texts));
               
                speechRecognitionEngine.LoadGrammar(wordsList);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Gets the known command.
        /// </summary>
        /// <param name="command">The order.</param>
        /// <returns></returns>
        private string getKnownTextOrExecute(string command)
        {
            try
            {
                var cmd = words.Where(c => c.kataUcapan == command).First();
               
           
                 return cmd.kata;
                
            }
            catch (Exception)
            {
                return command;
            }
        }

        #endregion

        #region speechEngine events

        /// <summary>
        /// Handles the SpeechRecognized event of the engine control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Speech.Recognition.SpeechRecognizedEventArgs"/> instance containing the event data.</param>
        void engine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            txtSpoken.Text += getKnownTextOrExecute(e.Result.Text);
            sendKata += getKnownTextOrExecute(e.Result.Text); 
            scvText.ScrollToEnd();
        }

        /// <summary>
        /// Handles the AudioLevelUpdated event of the engine control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Speech.Recognition.AudioLevelUpdatedEventArgs"/> instance containing the event data.</param>
        void engine_AudioLevelUpdated(object sender, AudioLevelUpdatedEventArgs e)
        {
            prgLevel.Value = e.AudioLevel;
        }

        #endregion

        #region window closing

        /// <summary>
        /// Handles the Closing event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // unhook events
            speechRecognitionEngine.RecognizeAsyncStop();
            // clean references
            speechRecognitionEngine.Dispose();
        }

        #endregion

        #region GUI events

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Mulai_Click(object sender,RoutedEventArgs e)
        {
            speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
        }

        private void kirimSocket(object sender,RoutedEventArgs e)
        {
            sendSocket();
        }
        #endregion
    }
}
