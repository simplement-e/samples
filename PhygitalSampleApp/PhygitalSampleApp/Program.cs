using Equihira.Phygital.Client;
using Equihira.Phygital.Client.Business;
using Equihira.Phygital.Client.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhygitalSampleApp
{
    /// <summary>
    /// Pour faire fonctionner cette application, vous aurez besoin :
    /// - d'avoir activé le module "borne tactile" ou "tablettes vendeur"
    /// - d'avoir procédé à la configuration complète de ce module
    /// - de créer une application connectée "TestApp" et d'en recopier la clef
    ///   dans l'Init ci-dessous
    /// - de remplacer l'url de votre serveur ci-dessous
    /// - et de créer le poste correspondant au nom de cette machine
    /// </summary>
    class Program
    {
        private static readonly string nomPoste = Environment.MachineName;
        private static readonly string url = "http://votreserver";

        static void Main(string[] args)
        {
            MyApp.Init<MyApplicationModelBase>(null, "TestApp", "<MettezLaClefDeVotreApplicationConnectee>", null);

            Do();

            Console.ReadLine();
        }

        private async static void Do()
        {
            ApplicationModelBase model = ApplicationModelBase.Instance as ApplicationModelBase;

            var bll = new IdentityBll();

            if (!MyApp.HasServerUrl())
            {
                Console.WriteLine("La connexion n'est pas configurée, configuration vers demo");
                Console.WriteLine("----");
                Console.WriteLine("essai de connexion");
                var t = await MyApp.TryConnectAsync(url, nomPoste, true);
                if (t == IdentityBll.ConnectionResult.DeviceUnknown)
                {
                    Console.WriteLine("Veuillez créer le poste dans votre gestion commerciale");
                    Console.WriteLine("-----");
                    Console.WriteLine("fin");
                    return;
                }
                else if (t == IdentityBll.ConnectionResult.Connected)
                {
                    try
                    {
                        await MyApp.SaveConnectParameters(url, nomPoste);
                    }
                    catch
                    {

                    }
                }
                else
                {
                    Console.WriteLine("Impossible de vous connecter");
                    Console.WriteLine("-----");
                    Console.WriteLine("fin");
                    return;
                }
            }

            bool b = await MyApp.Connect(true);
            if (!b)
            {
                Console.WriteLine("Non connecté");
                return;
            }
            Console.WriteLine("Connecté");
            Console.Write("device guid = ");
            Console.WriteLine(model.DeviceGuid);
            Console.Write("magasin = ");
            Console.WriteLine(model.Store.Name);
            Console.WriteLine("----");

            Console.WriteLine("Catégories racines : ");
            var bllC = new CatalogueBll();
            var cfg = await bllC.GetConfig();
            foreach (var c in cfg.Categories)
            {
                Console.WriteLine(c.Name);
            }
            Console.WriteLine("----");
        }

    }


    public class MyApplicationModelBase : ApplicationModelBase
    {

    }

    public static class MyApp
    {
        /// <summary>
        /// Le data-model principal de l'application
        /// </summary>
        public static MyApplicationModelBase Model { get { return ApplicationModelBase.Instance as MyApplicationModelBase; } }

        private static bool _otherInitDone = false;
        private static string _deviceName = null;


        /// <summary>
        /// Méthode d'initialisation de l'application
        /// </summary>
        /// <typeparam name="T">Le type de data-model pour l'application</typeparam>
        /// <param name="urlServer">L'url du serveur</param>
        /// <param name="appId">L'id de votre application</param>
        /// <param name="appKey">La clef secrete associée à votre application</param>
        /// <param name="deviceName">Le nom sous lequel ce poste doit être enregistré</param>
        /// <param name="model">Le data-model à associer à l'application</param>
        /// <returns><c>true</c> si la connexion à pu s'établir</returns>
        /// <remarks>Si vous obtenez <c>false</c> en retour de cette méthode, vous n'aurez aucun accès
        /// aux web-services et devrez ré-essayer après avoir vérifié vos paramètres, la possibilité
        /// de vous connecter au serveur et que le device est bien enregistré.</remarks>
        public static void Init<T>(string urlServer, string appId, string appKey, string deviceName, T model)
            where T : ApplicationModelBase, new()
        {
            if (!_otherInitDone)
            {
                _otherInitDone = true;
                try
                {
                    _deviceName = deviceName;
                    DataService.SetImpl(new NetDataServiceImpl());
                    ApplicationModelBase.Init<T>(model);
                    WebServiceConnectionService.Init(urlServer, appId, appKey);
                }
                catch
                {
                    _otherInitDone = false;
                }
            }

        }


        /// <summary>
        /// Méthode d'initialisation de l'application en instanciant un nouvel objet
        /// du type donné
        /// </summary>
        /// <typeparam name="T">Le type de data-model pour l'application</typeparam>
        /// <param name="urlServer">L'url du serveur</param>
        /// <param name="appId">L'id de votre application</param>
        /// <param name="appKey">La clef secrete associée à votre application</param>
        /// <param name="deviceName">Le nom sous lequel ce poste doit être enregistré</param>
        /// <returns><c>true</c> si la connexion à pu s'établir</returns>
        /// <remarks>Si vous obtenez <c>false</c> en retour de cette méthode, vous n'aurez aucun accès
        /// aux web-services et devrez ré-essayer après avoir vérifié vos paramètres, la possibilité
        /// de vous connecter au serveur et que le device est bien enregistré.</remarks>
        public static void Init<T>(string urlServer, string appId, string appKey, string deviceName)
            where T : ApplicationModelBase, new()
        {
            T val = null;
            if (ApplicationModelBase.Instance != null)
                val = ApplicationModelBase.Instance as T;
            if (val == null)
                val = new T();
            Init(urlServer, appId, appKey, deviceName, val);
        }

        /// <summary>
        /// Connecte l'application
        /// </summary>
        /// <param name="startCreationIfInexistant">si <c>true</c>, un todo de demande
        /// de création sera ajouté dans votre gestion commerciale</param>
        /// <returns><c>true</c> si la connexion a pu être établie</returns>
        public static async Task<bool> Connect(bool startCreationIfInexistant = false)
        {
            IdentityBll bll = new IdentityBll();
            bool b = await bll.ConnectAsync(_deviceName, ApplicationModelBase.Instance);
            return b;
        }


        /// <summary>
        /// Effectue un test de connexion. Attention, même en cas de réussite,
        /// la connexion n'est pas permanente. Vous devez appeler <see cref="Connect"/>
        /// pour établir la connexion définitive.
        /// </summary>
        /// <param name="startCreationIfInexistant">si <c>true</c>, un todo de demande
        /// de création sera ajouté dans votre gestion commerciale</param>
        /// <returns><c>NotConnected</c> si la connexion a échoué, <c>DeviceUnknown</c> si le 
        /// device n'est pas enregistré ou <c>Connected</c> si la connexion a 
        /// été établie</returns>
        internal static async Task<IdentityBll.ConnectionResult> TryConnectAsync(string url, string nomPoste, bool createIfInexistant)
        {
            return await new IdentityBll().TryConnectAsync(url, nomPoste, createIfInexistant);
        }

        /// <summary>
        /// Vérifie si on a une url serveur enregistrée
        /// </summary>
        /// <returns><c>false</c> si aucune url n'est enregistrée</returns>
        internal static bool HasServerUrl()
        {
            return !string.IsNullOrEmpty(WebServiceConnectionService.Url);
        }

        /// <summary>
        /// Enregistre l'url et le nom de poste pour
        /// les connexions futures
        /// </summary>
        /// <param name="url">url du serveur</param>
        /// <param name="nomPoste">nom du poste</param>
        internal static async Task SaveConnectParameters(string url, string nomPoste)
        {
            await new IdentityBll().SaveConnectParameters(url, nomPoste);
        }

    }
}
