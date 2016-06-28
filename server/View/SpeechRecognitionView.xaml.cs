﻿using Microsoft.Speech.Recognition;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Kinect2Server.View
{
    /// <summary>
    /// Interaction logic for SpeechRecognitionView.xaml
    /// </summary>
    public partial class SpeechRecognitionView : UserControl
    {

        private SpeechRecognition sr;
        private MainWindow mw;

        public SpeechRecognitionView()
        {
            this.mw = (MainWindow)Application.Current.MainWindow;
            this.sr = this.mw.SpeechRecogniton;
            InitializeComponent();
        }

        // Turn off or on the speech recognition 
        private void switchSR(object sender, RoutedEventArgs e)
        {
            clearRecognitionText();

            if (!sr.isSpeechEngineSet() || !sr.anyGrammarLoaded())
            {
                setButtonOn(this.stackSR);
                LoadGrammarFile(sender, e);
            }
            else if (sr.anyGrammarLoaded())
            {
                setButtonOff(this.stackSR);
                this.status.Text = Properties.Resources.ZzZz;
                sr.unloadGrammars();
                this.sr.SpeechRecognitionEngine.RecognizeAsyncStop();
            }
            else
            {
                setButtonOn(this.stackSR);
                this.status.Text = Properties.Resources.GoOn;
                sr.loadGrammar();
                this.sr.SpeechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        public void clearRecognitionText()
        {
            this.lastSemantics.Text = "";
            this.lastSentence.Text = "";
        }

        public void setButtonOff(StackPanel stack)
        {
            Dispatcher.Invoke(() =>
            {
                Image img = new Image();
                stack.Children.Clear();
                img.Source = new BitmapImage(new Uri(@"../Images/switch_off.png", UriKind.Relative));
                stack.Children.Add(img);
            });
            
        }

        public void setButtonOn(StackPanel stack)
        {
            Dispatcher.Invoke(() =>
            {
                Image img = new Image();
                stack.Children.Clear();
                img.Source = new BitmapImage(new Uri(@"../Images/switch_on.png", UriKind.Relative));
                stack.Children.Add(img);
                this.browse.IsEnabled = true;
            });
            
        }

        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // HACK : do not allow speech to be recognized one second after last tts
            TimeSpan ts = new TimeSpan(0,0,1);
            if (this.sr.LastTTS.Add(ts).CompareTo(DateTime.Now) < 0)
            {
                if (e.Result.Confidence >= this.sr.ConfidenceThreshold)
                {
                    List<String> contentSemantic = this.sr.SpeechRecognized(e.Result.Semantics, e.Result.Text);
                    //Update the text
                    this.Dispatcher.Invoke(() =>
                    {
                        this.lastSemantics.Text = "";
                        this.lastSentence.Text = e.Result.Text;
                        this.lastSemantics.Text = "";
                        if (contentSemantic != null)
                        {
                            for (int i = 0; i < contentSemantic.Count; i++)
                            {
                                this.lastSemantics.Text += contentSemantic[i];
                            }
                        }
                    });
                }

            }
        }

        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.lastSentence.Text = Properties.Resources.NoWordsRecognized;
                this.lastSemantics.Text = "";
            });
        }


        // Update the XML file when the user opens a file
        private void LoadGrammarFile(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".xml";
            dlg.Filter = "XML Files (.xml)|*.xml";

            // Display OpenFileDialog
            Nullable<bool> result = dlg.ShowDialog();

            String message = "";

            // Get the selected file path and set it in location if it is different from the actual file
            if (result == true && this.sr.isFileNew(dlg.FileName))
            {
                clearRecognitionText();

                // Create a new grammar from the file loaded
                message = this.sr.createGrammar(dlg.FileName, dlg.SafeFileName);

                this.sr.FileName = dlg.SafeFileName;
                this.RefreshGrammarFile();
                setButtonOn(this.stackSR);

            }
            // Turn the button off if the FileDialog is closed and the SpeechEngine doesn't have any grammar loaded
            else if (result == false && !this.sr.anyGrammarLoaded())
                setButtonOff(this.stackSR);

            // If there is an error while creating the grammar, a message is written and the button is turned off
            if (!this.sr.anyGrammarLoaded() || this.sr.CurrentLanguage.Equals(""))
            {
                setButtonOff(this.stackSR);
                this.status.Text = message;
                return;
            }
            else
                this.status.Text = Properties.Resources.GoOn;
            
            if (this.sr.isSpeechEngineSet())
                this.addlist();
        }

        public void RefreshGrammarFile()
        {
            Dispatcher.Invoke(() =>
            {
                this.currentFile.Text = this.sr.FileName;
                switch (sr.CurrentLanguage)
                {
                    case "en-US":
                        this.currentLanguage.Text = Properties.Resources.AmericanEnglish;
                        break;

                    case "fr-FR":
                        this.currentLanguage.Text = Properties.Resources.French;
                        break;

                    //Add as much languages as you want provided that the recognized is installed on the PC
                    //You also have to add a Ressource in the file Ressources.resx for exemple :
                    //        - Call your ressource German and fill it with "Current language : German"
                }
            });
        }

        public void addlist()
        {
            this.sr.addSRListener(this.SpeechRecognized, this.SpeechRejected);
        }

        private void SubmitConfidence(object sender, RoutedEventArgs e)
        {
            if (this.confidenceSelector.Value == null)
            {
                this.RefreshConfidenceSelectorValue();
            }
            else
            {
                sr.ConfidenceThreshold = (float)this.confidenceSelector.Value;
                this.clearRecognitionText();
            }
        }

        public void RefreshConfidenceSelectorValue()
        {
            Dispatcher.Invoke(() =>
            {
                this.confidenceSelector.Value = (double)Math.Round(sr.ConfidenceThreshold,1);
            });
        }

        private void submitListeningPort(object sender, RoutedEventArgs e)
        {
            if (this.listeningPortSelector.Value == null)
                this.listeningPortSelector.Value = 33405;
            else
            {
                sr.ListeningPort = ((int)this.listeningPortSelector.Value);
            }
        }
    }
}
