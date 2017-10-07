using CIE.MRTD.SDK.EAC;
using CIE.MRTD.SDK.PCSC;
using CIE.MRTD.SDK.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                SmartCard smc = new SmartCard();

                smc.onRemoveCard += new CardEventHandler((r) => { label1.Text = "Waiting card..."; });
                
                // All'inserimento del documento aggiorno la label e avvio la lettura
                smc.onInsertCard += new CardEventHandler((r) =>
                {
                    readCardData(smc, r);
                });

                smc.onRemoveCard += new CardEventHandler((r) =>
                {
                    smc.EndTransaction(Disposition.SCARD_LEAVE_CARD);
                 
                });

                // Imposto l'interfaccia per l'invio delle notifiche. In questo modo posso interagire con
                // il form senza dover usare delle Invoke.
                smc.InterfaceForEvents = this;

                // Avvio il monitoraggio dei lettori
                smc.StartMonitoring(true);
                smc.InterfaceForEvents = this;

            } catch(Exception e1)
            {
                txtStatus.Text += "err2 " + e1.Message;
            }
        }

         
        void readCardData(SmartCard smc, string r)
        {
            try
            {

                // label1.Text = "Reading...";
                smc = new SmartCard();
              //  Thread.Sleep(3000);
                // siamo all'interno dell'event handler del form, quindi per aggiornare la label devo eseguire il Message Loop
                Application.DoEvents();
             //   Thread.Sleep(3000);
            // avvio la connessione al lettore richiedendo l'accesso esclusivo al chip
            if (!smc.Connect(r, Share.SCARD_SHARE_EXCLUSIVE, Protocol.SCARD_PROTOCOL_T1))
            {
                System.Diagnostics.Debug.WriteLine("Errore in connessione: " + smc.LastSCardResult.ToString("X08"));
                label1.Text = "Errore in connessione: " + smc.LastSCardResult.ToString("X08");
                return;
            }

            // Creo l'oggetto EAC per l'autenticazione e la lettura, passando la smart card su cui eseguire i comandi
            EAC a = new EAC(smc);
            // Verifico se il chip è SAC
            if (a.IsSAC())
            {
                // Effettuo l'autenticazione PACE.
                // In un caso reale prima di avvare la connessione al chip dovrei chiedere all'utente di inserire il CAN  
                txtStatus.Text += "chip SAC - PACE" + "\n";
                //a.PACE("5678");
                a.PACE("641230", new DateTime(2022, 12, 30), "CA00000AA");

            }
            else
            {
                // Per fare BAC dovrei fare la scansione dell'MRZ e applicare l'OCR all'imagine ottenuta. In questo caso ritorno errore.
                a.BAC("641230", new DateTime(2022, 12, 30), "CA00000AA");                    //a.BAC()
                // label1.Text = "BAC non disponibile";
                txtStatus.Text += "chip BAC" + "\n";

            }

                
           
            // Per poter fare la chip authentication devo prima leggere il DG14
           var dg14 = a.ReadDG(DG.DG14);

            // Effettuo la chip authentication
            a.ChipAuthentication();

            txtStatus.Text += new ByteArray(a.ReadDG((DG)11)).ToString().Replace(" ","");
           
                // Leggo il DG2 contenente la foto
            var dg2 = a.ReadDG(DG.DG2);

            // Disconnessione dal chip
            smc.Disconnect(Disposition.SCARD_RESET_CARD);

                // Aggiorno la laber del form
                // label1.Text = "OK!";


            }
            catch(Exception e)
            {
                txtStatus.Text += "Eccezione: " + e.Message;
            }
        }

    }
}
