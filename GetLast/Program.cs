using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GetLast
{
    class Program
    {
        #region Variáveis

        static List<string> pastasVersoes = new List<string>();
        static string InicioFrase = "***** ";
        static object locker = new object();
        static Dictionary<string, Parametros> parametros = new Dictionary<string, Parametros>();
        static string trunkMainDisco = @"Trunk\Main";
        static string trunkMainTeam = @"Trunk/Main";
        static string branchDisco = @"Branch\{0}";
        static string branchTeam = @"Branch/{0}";
        static bool trunk = false;
        static string versaoDisco = string.Empty;
        static string versaoTeam = string.Empty;
        static List<string> linhasArquivo = new List<string>();

        /*
         2019 = C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe
         2019 = C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\TF.exe
         2017 = C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe
         2017 = C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\TF.exe
         */

        static string MSBuildLocation = string.Empty;
        static string MSTFLocation = string.Empty;
        static string LocalVersoes = string.Empty;

        private static int ct = 0;
        public static int ContadorThread
        {
            set
            {
                lock (locker)
                {
                    ct = value;
                }
            }
            get
            {
                return ct;
            }
        }

        #endregion

        static void Main(string[] args)
        {
            LerArquivo();

            Console.WriteLine(InicioFrase);
            Console.WriteLine("**************************************Get Last by Jefers********************************************");
            Console.WriteLine(InicioFrase);
            Console.WriteLine(InicioFrase);
            Console.WriteLine(InicioFrase + "(Ctrl + c) para finalizar");
            Console.WriteLine(InicioFrase);
            Console.WriteLine(InicioFrase);

            DefinirVersao();
            ListarProjeto();
            PrepararBaixar();

            Console.WriteLine(InicioFrase);
            Console.WriteLine(InicioFrase);
            Console.WriteLine("**************************************Project Compiler by Jefers********************************************");
            Console.WriteLine(InicioFrase + "Tecle para finalizar");
            Console.Read();
        }

        #region Preparar

        private static void LerArquivo()
        {
            string nomeArquivo = "Projetos.txt";

            string file = File.ReadAllText(nomeArquivo);

            using (StreamReader s = new StreamReader(nomeArquivo))
            {
                linhasArquivo.Clear();
                while (!s.EndOfStream)
                {
                    string linha = s.ReadLine();

                    if (linha.ToUpper().StartsWith("MSBUILDPATH"))
                    {
                        MSBuildLocation = linha.Substring(12);
                    }
                    else if (linha.ToUpper().StartsWith("MSTFPATH"))
                    {
                        MSTFLocation = linha.Substring(9);
                    }
                    else if (linha.ToUpper().StartsWith("VERSIONPATH"))
                    {
                        LocalVersoes = linha.Substring(12);
                    }

                    else
                    {
                        linhasArquivo.Add(linha);
                    }
                }
            }
        }

        private static void DefinirVersao()
        {
            Dictionary<int, string> versoesDisponiveis = new Dictionary<int, string>();
            string pastaWorkspace = LocalVersoes;
            if (Directory.Exists(pastaWorkspace))
            {
                Console.WriteLine(InicioFrase + "Versões encontradas: ");

                DirectoryInfo di = new DirectoryInfo(pastaWorkspace);
                var diretorios = di.GetDirectories();

                int cod = 1;
                foreach (var diretorio in diretorios)
                {
                    Console.WriteLine(InicioFrase + string.Format("{0} - {1}", cod, diretorio.Name));
                    versoesDisponiveis.Add(cod, diretorio.Name);
                    pastasVersoes.Add(diretorio.Name);
                    cod++;
                }

                if (versoesDisponiveis.Count == 0)
                {
                    Console.WriteLine(InicioFrase + "Versões encontradas = 0 ");
                }
            }
        }

        private static void ListarProjeto()
        {
            int cod = 1;
            foreach (var linha in linhasArquivo)
            {
                Parametros p = new Parametros();

                string[] array = linha.Split(',');

                if (array.Length > 0)
                {
                    p.Codigo = cod.ToString();

                    foreach (var versao in pastasVersoes)
                    {
                        versaoTeam = string.Format(branchTeam, versao);
                        p.LocaisTeamFoundation.Add(versao, string.Format(array[1], versaoTeam));
                        p.Projeto = array[1].Substring(array[1].LastIndexOf(@"/") + 1);
                    }

                    if (!p.LocaisTeamFoundation.ContainsKey("Trunk"))
                    {
                        p.LocaisTeamFoundation.Add("Trunk", string.Format(array[1], trunkMainTeam));
                    }
                }

                if (!parametros.ContainsKey(p.Codigo))
                {
                    parametros.Add(p.Codigo.Trim(), p);
                }

                cod++;
            }
        }

        private static List<Parametros> EscolherProjeto()
        {
            List<Parametros> listaParam = new List<Parametros>();
            Console.WriteLine(Environment.NewLine + InicioFrase + "* + parte do nome");
            Console.WriteLine(InicioFrase + ", separar os código");
            Console.WriteLine(InicioFrase + "; buscar tudo");
            Console.WriteLine(InicioFrase);
            Console.Write(InicioFrase + "Digite o(s) código(s) da solution: ");
            List<string> solutions = new List<string>();
            string solution = Console.ReadLine();

            if (solution.ToUpper().StartsWith("*"))
            {
                solution = solution.Replace("*", "");
                if (solution.Contains(","))
                {
                    var arraySolution = solution.Split(new char[] { ',' });
                    foreach (string parteSolution in arraySolution)
                    {
                        foreach (var p in parametros)
                        {
                            if (p.Value.Projeto.ToUpper().Contains(parteSolution.ToUpper()))
                            {
                                listaParam.Add(p.Value);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var p in parametros)
                    {
                        if (p.Value.Projeto.ToUpper().Contains(solution.ToUpper()))
                        {
                            listaParam.Add(p.Value);
                            break;
                        }
                    }
                }

                if (listaParam.Count == 0)
                {
                    Console.Write(InicioFrase + "Código inválido!");
                    return EscolherProjeto();
                }
            }

            else if (solution.ToUpper().Equals(";"))
            {
                foreach (var p in parametros)
                {
                    listaParam.Add(p.Value);
                }
            }

            else
            {
                if (solution.Contains(","))
                {
                    var arraySolution = solution.Split(new char[] { ',' });
                    for (int i = 0; i < arraySolution.Length; i++)
                    {

                        solution = arraySolution[i];
                        if (solution.Trim().Length > 0)
                        {
                            if (parametros.ContainsKey(solution))
                            {
                                var parametro = parametros[solution];
                                listaParam.Add(parametros[solution]);
                            }
                            else
                            {
                                Console.Write(InicioFrase + "Algum Código inválido!");
                                return EscolherProjeto();
                            }
                        }

                        if (listaParam.Count == 0)
                        {
                            Console.Write(InicioFrase + "Nenhum Código informado!");
                            return EscolherProjeto();
                        }
                    }
                }
                else
                {
                    if (parametros.ContainsKey(solution.Trim()))
                    {
                        listaParam.Add(parametros[solution]);
                    }
                    else
                    {
                        Console.Write(InicioFrase + "Código inválido!");
                        return EscolherProjeto();
                    }
                }
            }

            if (listaParam.Count > 0)
            {
                Console.WriteLine(InicioFrase + "Projeto(s) a baixar: ");
                foreach (var parametro in listaParam)
                {
                    Console.WriteLine(InicioFrase + parametro.Projeto);
                }

                Console.Write(InicioFrase + "Confirme o get do(s) projeto(s): " + "? S ou N: ");

                var resposta = Console.ReadLine();

                if (resposta.ToUpper() != "S")
                    EscolherProjeto();
            }

            return listaParam;
        }

        #endregion

        #region Get

        private static void PrepararBaixar()
        {
            try
            {
                ListaEConfigurarCompilacao();
            }
            catch (ArgumentException ex)
            {
                Console.Write(InicioFrase);
                Console.Write(InicioFrase + ex.Message);
                Console.Write(InicioFrase);
            }
        }

        private static void ListaEConfigurarCompilacao()
        {
            Console.WriteLine(InicioFrase);
            Console.Write(InicioFrase + "Listagem das solutions: " + Environment.NewLine);
            Console.WriteLine(InicioFrase);

            foreach (var parametro in parametros)
            {
                Console.WriteLine(InicioFrase + InicioFrase + string.Format("{0} [{1}]", parametro.Value.Codigo, parametro.Value.Projeto));
            }

            List<Parametros> solutionsCompilar = EscolherProjeto();

            ProcessarGet(solutionsCompilar);

        }

        #endregion

        #region Get Last

        private static void ProcessarGet(List<Parametros> solutionsCompilar)
        {
            int getCount = 1;
            int maxGet = 7;
            foreach (var s in solutionsCompilar)
            {
                foreach (var localTeamFoudation in s.LocaisTeamFoundation)
                {
                    ContadorThread++;

                    Parametros solution = new Parametros();
                    solution.CurrenteTeamFoundation = localTeamFoudation;
                    solution.Projeto = s.Projeto;

                    ParameterizedThreadStart p = new ParameterizedThreadStart(GetLast);
                    Thread t = new Thread(p);
                    t.Start(solution);

                    if (getCount == maxGet)
                    {
                        while (ContadorThread > 0)
                        {
                            Thread.Sleep(300);
                        }

                        getCount = 0;
                    }

                    getCount++;
                }
            }

            while (ContadorThread > 0)
            {
                Thread.Sleep(300);
            }
        }

        private static void GetLast(object objSolution)
        {
            Parametros solution = (Parametros)objSolution;

            //var processo = Process.Start(@"C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\TF.exe", string.Format(@"get {0}", solution.LocalTeamFoundation) + @"  /force /recursive");
            var processo = Process.Start(MSTFLocation, string.Format(@"get {0}", solution.CurrenteTeamFoundation.Value) + @"  /force /recursive");
            Console.WriteLine(InicioFrase);
            Console.WriteLine(InicioFrase + string.Format("Get no Projeto {0} - Versão {1}", solution.Projeto, solution.CurrenteTeamFoundation.Key));
            Console.WriteLine(InicioFrase + "Aguarde...");

            processo.WaitForExit();

            Console.WriteLine(InicioFrase);
            Console.WriteLine(InicioFrase + $"Get {solution.Projeto} {solution.CurrenteTeamFoundation.Key} Done");

            ContadorThread--;
        }

        #endregion
    }

    class Parametros
    {
        public Parametros()
        {
            VoltarNelaNoFinal = false;
            NBuild = 0;
            Codigo = string.Empty;
            LocaisTeamFoundation = new Dictionary<string, string>();
            Erro = new StringBuilder();
        }

        public bool Builded { get; set; }
        public int NBuild { get; set; }
        public string Codigo { get; set; }
        public Dictionary<string, string> LocaisTeamFoundation { get; set; }
        public StringBuilder Erro { get; set; }
        public KeyValuePair<string, string> CurrenteTeamFoundation { get; set; }
        public string LocalDisco { get; set; }
        public string LocalTeamFoundation { get; set; }

        public string Projeto { get; set; }

        public bool VoltarNelaNoFinal { get; set; }

        public void ZerarParametros()
        {
            this.VoltarNelaNoFinal = false;
        }
    }
}
