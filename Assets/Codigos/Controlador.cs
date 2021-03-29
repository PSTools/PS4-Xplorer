using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using SharpCompress.Common;
using System;
using UnityEngine.Networking;


public class Controlador : MonoBehaviour
{

    [DllImport("universal")]
    private static extern int FreeUnjail(int FWVersion);

    [DllImport("universal")]
    private static extern int Temperature();

    [DllImport("universal")]
    private static extern int FreeFTP();

    [DllImport("universal")]
    private static extern int FreeMount();

    //allows you to get the firmware version of the console
    //will return 5.05 ext 
    [DllImport("universal")]
    private static extern UInt16 get_firmware();

    //[DllImport("universal", CallingConvention = CallingConvention.Cdecl)]
    //private static extern IntPtr PupDecrypt([MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport("universal")]
    private static extern int InstallPKG(string path, string name, string imgpath);
    [DllImport("universal")]
    private static extern int UnloadPKGModule();

    [DllImport("universal")]
    private static extern int SendMessageToPS4(string Message);


    public static Controlador instancia;

    public enum ActionsWhichRequiresKeyboard
    {
        Nothing,
        CreateFolder,
        RenameFile,
        CrearFichero,
        DescargarFichero
    };

    public enum Sobreescribir
    {
        Stop,
        Si,
        SiTodo,
        No,
        Cancel
    };

    [HideInInspector]
    public ActionsWhichRequiresKeyboard FileFolderAction;
    public Sobreescribir AccionSobrescribir = Sobreescribir.Stop;

    public int Idioma = 3;
    public Text txtCamino;
    public Text txtTemperatura;
    public Text txtCantidad;
    public Text txtLog;
    private string LOG = "";
    private string _s_ = "/"; // para PS4 poner "/" y para pruebas en Windows "\\"

    private int Nivel = 0;
    private List<Vector2> UltimaPos = new List<Vector2>();

    public Image ImagenFondo;
    public GameObject PanelOpciones;
    public GameObject PanelOpcionesAvanzadas;
    public GameObject PanelImagen;
    public Image PanelImagenImagen;
    public GameObject PanelPKG;
    public GameObject PanelEditorTexto1;
    public GameObject PanelEditorTexto2;
    public GameObject PanelDescompactador;
    public GameObject PanelSelecionUsuario;
    public AudioSource miAudioSource;

    public GameObject PanelCopiando;
    public Slider miSlider2;
    public Text TextPorciento;
    public GameObject BotonCancelar;
    bool CancelarPegado = false;

    public GameObject PanelTeclado;
    public GameObject PanelMensaje;
    public Text textoOptions;
    public Sprite instrucciones;
    public Font arialJapon;
    public Font arialChina;
    public Font arialKorea;
    public SonyPS4CommonDialog KeyBoardManagerScr;

    public GameObject carpetasPrefab;
    public GameObject carpetasPrefabBlackList;
    public GameObject carpetasPrefabEspecial;
    public GameObject ficherosPrefab;
    public GameObject ficherosPrefabMP3;
    public GameObject ficherosPrefabMP4;
    public GameObject ficherosPrefabFOT;
    public GameObject ficherosPrefabTXT;
    public GameObject ficherosPrefabSFO;
    public GameObject ficherosPrefabPKG;
    public GameObject ficherosPrefabRAR;
    public GameObject ficherosPrefabTHEME;
    public GameObject ficherosPrefabAVATAR;
    public GameObject ficherosPrefabPUP;

    public Sprite carpetasPrefab_Spr;
    public Sprite carpetasPrefabBlackList_Spr;
    public Sprite carpetasPrefabEspecial_Spr;
    public Sprite ficherosPrefab_Spr;
    public Sprite ficherosPrefabMP3_Spr;
    public Sprite ficherosPrefabMP4_Spr;
    public Sprite ficherosPrefabFOT_Spr;
    public Sprite ficherosPrefabTXT_Spr;
    public Sprite ficherosPrefabSFO_Spr;
    public Sprite ficherosPrefabPKG_Spr;
    public Sprite ficherosPrefabRAR_Spr;
    public Sprite ficherosPrefabTHEME_Spr;
    public Sprite ficherosPrefabAVATAR_Spr;
    public Sprite IconoOpciones;
    public Sprite IconoCortar;
    public Sprite IconoCopiar;
    public Sprite IconoPegar;
    public Sprite IconoRenombrar;
    public Sprite IconoEliminar;
    public Sprite IconoFTP;
    public Sprite IconoRW;
    public Sprite IconoHOME;
    public Sprite IconoCuadrado;
    public Sprite ImagenDefectoOverwrite;

    public ScrollRect scrollRect;
    public RectTransform contentPanel;

    private List<GameObject> ObjetosCreados = new List<GameObject>();
    private List<string> TODOS = new List<string>();

    private int Posicion = 0;
    private string[] scaneo;

    public string camino = "/";
    private bool Paso = true;
    private bool EnOpciones = false;
    private bool EnImagen = false;
    private bool EnEditorTexto = false;
    private bool Pegando = false;
    private bool EnTeclado = false;
    private bool Extrayendo = false;

    public bool Multiseleccion = false;
    private bool MultiseleccionCopiada = false;
    private bool MultiseleccionCortada = false;
    private List<int> ObjetosSelecionados = new List<int>();
    private List<string> ObjetosCaminos = new List<string>();

    public GameObject PanelVideo;
    public bool EnVideo = false;

    bool AplicarTema = false;
    bool AplicarAvatar = false;

    // cadenas para la traduccion...
    string CadenaMoviendo = "Moving...";
    string CadenaCopiando = "Copying...";
    string CadenaNoHasCopiado = "You have not cut or copied anything to paste";
    string CadenaErrorCrearCarpeta = "The folder you tried to create already exists";
    string CadenaFtpActivo = "FTP is Active";
    string CadenaRwActivo = "Full R/W permisions in systems folders (dangerous, be careful !)";
    string CadenaHomeSalvado = "Saved this path as ''Home'' (press R3 to return here at any time)";
    string CadenaNoHome = "Can't reach";
    string CadenaElemento = "element";
    string CadenaElementos = "elements";
    string CadenaExtraer = "Extract";
    string CadenaEspere = "Wait...";
    string CadenaAvatarInstalado = "Avatar installed!";
    string CadenaErrorCrearFichero = "The file you tried to create already exists";
    string CadenaInstalar = "Install PKG";

    public int FW = 0;

    public void Awake()
    {
        instancia = this;
    }

    void OnEnable()
    {
        SonyPS4CommonDialog.OnFolderCreate += NuevaCarpetaAccion;
        SonyPS4CommonDialog.OnRenameFile += RenombrarAccion;
        SonyPS4CommonDialog.OnCreateFile += NuevoFicheroAccion;
        SonyPS4CommonDialog.OnDownloadFile += DescargarFicheroAction;
    }

    void OnDisable()
    {
        SonyPS4CommonDialog.OnFolderCreate -= NuevaCarpetaAccion;
        SonyPS4CommonDialog.OnRenameFile -= RenombrarAccion;
        SonyPS4CommonDialog.OnCreateFile -= NuevoFicheroAccion;
        SonyPS4CommonDialog.OnDownloadFile -= DescargarFicheroAction;
    }

    void Start()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
        CargarTema();

        switch (Idioma)
        {
            case 8: //SystemLanguage.Portuguese:
                CadenaMoviendo = "Movendo...";
                CadenaCopiando = "Copiando...";
                CadenaNoHasCopiado = "Você não cortou nem copiou nada para colar";
                textoOptions.text = "OPTIONS\npara instruções";
                CadenaErrorCrearCarpeta = "A pasta que voçê tentou criar já existe";
                CadenaFtpActivo = "FTP Ativado";
                CadenaRwActivo = "Permissões R/W completas em pastas de sistemas (perigoso, tenha cuidado!)";
                CadenaHomeSalvado = "Salvei esse caminho como '' Inicio '' (pressione R3 para retornar aqui a qualquer momento)";
                CadenaNoHome = "Não alcança";
                CadenaElemento = "elemento";
                CadenaElementos = "elementos";
                BotonCancelar.GetComponentInChildren<Text>().text = "Cancelar";
                CadenaExtraer = "Extrair";
                CadenaEspere = "Esperar...";
                CadenaAvatarInstalado = "Avatar instalado!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Selecione o usuário";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Ok";
                CadenaErrorCrearFichero = "O arquivo que você tentou criar já existe";
                CadenaInstalar = "Instale PKG";
                break;
            case 9: //SystemLanguage.Spanish:
                CadenaMoviendo = "Moviendo...";
                CadenaCopiando = "Copiando...";
                CadenaNoHasCopiado = "No has cortado o copiado nada que pegar";
                textoOptions.text = "OPTIONS\npara instrucciones";
                CadenaErrorCrearCarpeta = "La carpeta que intentas crear ya existe";
                CadenaFtpActivo = "FTP Activado";
                CadenaRwActivo = "Permisos R/W completos en carpetas del sistema (peligro, tenga cuidado !)";
                CadenaHomeSalvado = "Salvada esta ruta como ''Home'' (presione R3 para volver aquí cuando lo desee)";
                CadenaNoHome = "No se puede alcanzar";
                CadenaElemento = "elemento";
                CadenaElementos = "elementos";
                BotonCancelar.GetComponentInChildren<Text>().text = "Cancelar";
                CadenaExtraer = "Extraer";
                CadenaEspere = "Espere...";
                CadenaAvatarInstalado = "Avatar instalado !";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Selecciona el perfil de usuario";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Ok";
                CadenaErrorCrearFichero = "El fichero que intentas crear ya existe";
                CadenaInstalar = "Instalar PKG";
                break;
            case 7: //SystemLanguage.Japanese:
                txtCamino.font = arialJapon;
                txtCantidad.font = arialJapon;
                txtLog.font = arialJapon;
                textoOptions.font = arialJapon;
                PanelCopiando.GetComponentInChildren<Text>().font = arialJapon;
                BotonCancelar.GetComponentInChildren<Text>().font = arialJapon;
                PanelDescompactador.transform.GetChild(1).GetComponentInChildren<Text>().font = arialJapon;
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().font = arialJapon;
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().font = arialJapon;
                PanelPKG.transform.GetChild(4).GetComponentInChildren<Text>().font = arialJapon;

                CadenaMoviendo = "移動しています...";
                CadenaCopiando = "コピーしています...";
                CadenaNoHasCopiado = "貼り付けるものを、切り取り又はコピーしていない";
                textoOptions.text = "OPTIONS\n操作方法";
                CadenaErrorCrearCarpeta = "作成しようとしたフォルダは既に存在します";
                CadenaFtpActivo = "FTPはアクティブです";
                CadenaRwActivo = "システムフォルダ内の完全な読み込み / 書き込みを許可（危険です。注意してください。）";
                CadenaHomeSalvado = "このパスをホームとして保存する(R3を押せばここに戻れます)";
                CadenaNoHome = "見つかりませんでした";
                CadenaElemento = "種類";
                CadenaElementos = "種類";
                BotonCancelar.GetComponentInChildren<Text>().text = "キャンセル";
                CadenaExtraer = "エキス";
                CadenaEspere = "待つ...";
                CadenaAvatarInstalado = "アバターがインストールされました！";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "ユーザーを選択";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Ok";
                CadenaErrorCrearFichero = "作成しようとしたファイルは既に存在します";
                CadenaInstalar = "PKGをインストールする";
                break;
            case 4: //SystemLanguage.French:
                CadenaMoviendo = "Bouger...";
                CadenaCopiando = "Copie en cours...";
                CadenaNoHasCopiado = "Vous n'avez rien àcouper ou coller";
                textoOptions.text = "OPTIONS\npour instruction";
                CadenaErrorCrearCarpeta = "Le dossier que vous essayez de créer existe deja";
                CadenaFtpActivo = "FTP est actif";
                CadenaRwActivo = "Accès complet R/W dans les fichiers du système (Dangereux, soyez prudents)";
                CadenaHomeSalvado = "Chemin sauvegardé en tant que page d’accueil (appuyez sur R3 pour revenir ici à n'importe quel moment)";
                CadenaNoHome = "Cible introuvable";
                CadenaElemento = "élément";
                CadenaElementos = "éléments";
                BotonCancelar.GetComponentInChildren<Text>().text = "Annuler";
                CadenaExtraer = "Extrait";
                CadenaEspere = "Attendez...";
                CadenaAvatarInstalado = "Avatar installé!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Sélectionnez l'utilisateur";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "D'accord";
                CadenaErrorCrearFichero = "Le fichier que vous avez essayé de créer existe déjà";
                CadenaInstalar = "Installez PKG";
                break;
            case 5: //SystemLanguage.German:
                CadenaMoviendo = "Verschiebe...";
                CadenaCopiando = "Kopiere...";
                CadenaNoHasCopiado = "Du hast nichts zum einfügen ausgeschnitten oder kopiert";
                textoOptions.text = "OPTIONS\nfür Anleitung";
                CadenaErrorCrearCarpeta = "Der ordner den du versuchst zu erstellen existiert bereits";
                CadenaFtpActivo = "FTP ist Aktiv";
                CadenaRwActivo = "Volle R/W rechte in System Ordner (Gefährlich sei vorsichtig!)";
                CadenaHomeSalvado = "Pfad wurde als ''Home'' gespeichert (R3 drücken um jederzeit auf ''Home'' zuzugreifen)";
                CadenaNoHome = "Ist nicht aufrufbar";
                CadenaElemento = "datei";
                CadenaElementos = "dateien";
                BotonCancelar.GetComponentInChildren<Text>().text = "Abbrechen";
                CadenaExtraer = "Extrakt";
                CadenaEspere = "Warten...";
                CadenaAvatarInstalado = "Avatar installiert!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Wählen Sie den Benutzer aus";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Ok";
                CadenaErrorCrearFichero = "Die Datei, die Sie erstellen wollten, ist bereits vorhanden";
                CadenaInstalar = "Installieren Sie PKG";
                break;
            case 10: //SystemLanguage.Ukrainian:
                CadenaMoviendo = "Переміщаю...";
                CadenaCopiando = "Копіюю...";
                CadenaNoHasCopiado = "Ви не вирізали та не скопіювали нічого щоб вставити";
                textoOptions.text = "OPTIONS\nдля інструкцій";
                CadenaErrorCrearCarpeta = "Теку яку ви намагались створити уже існує";
                CadenaFtpActivo = "FTP Включений";
                CadenaRwActivo = "Дозвіл на повне R/W в системних теках (не безпечно, будьте обережні!)";
                CadenaHomeSalvado = "Зберегти цей шлях як ''Додому'' (нажміть R3 що повернутись сюди будь-коли)";
                CadenaNoHome = "Не можу дістатись";
                CadenaElemento = "eлемент";
                CadenaElementos = "eлементи";
                BotonCancelar.GetComponentInChildren<Text>().text = "Відміна";
                CadenaExtraer = "Витяг";
                CadenaEspere = "Зачекайте...";
                CadenaAvatarInstalado = "Аватар встановлено!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Виберіть користувача";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Добре";
                CadenaErrorCrearFichero = "Файл, який ви намагалися створити, уже існує";
                CadenaInstalar = "Встановіть PKG";
                break;
            case 6: //SystemLanguage.Italian:
                CadenaMoviendo = "Spostamento in corso...";
                CadenaCopiando = "Copia in corso...";
                CadenaNoHasCopiado = "Non hai tagliato o copiato nulla da incollare";
                textoOptions.text = "OPTIONS\nper istruzioni";
                CadenaErrorCrearCarpeta = "La cartella che hai provato a creare esiste già";
                CadenaFtpActivo = "FTP è attivo";
                CadenaRwActivo = "Permessi R/W completi nelle cartelle di sistema (pericoloso, attenzione!)";
                CadenaHomeSalvado = "Salvato questo percorso come ''Home'' (premi R3 per tornare qui in qualsiasi momento)";
                CadenaNoHome = "Impossibile raggiungere";
                CadenaElemento = "elemento";
                CadenaElementos = "elementi";
                BotonCancelar.GetComponentInChildren<Text>().text = "Annulla";
                CadenaExtraer = "Estratto";
                CadenaEspere = "Apettare...";
                CadenaAvatarInstalado = "Avatar installato!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Seleziona l'utente";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Ok";
                CadenaErrorCrearFichero = "Il file che hai provato a creare esiste già";
                CadenaInstalar = "Installa PKG";
                break;
            case 2: //SystemLanguage.Chinese:
                txtCamino.font = arialChina;
                txtCantidad.font = arialChina;
                txtLog.font = arialChina;
                textoOptions.font = arialChina;
                PanelCopiando.GetComponentInChildren<Text>().font = arialChina;
                BotonCancelar.GetComponentInChildren<Text>().font = arialChina;
                PanelDescompactador.transform.GetChild(1).GetComponentInChildren<Text>().font = arialChina;
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().font = arialChina;
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().font = arialChina;
                PanelPKG.transform.GetChild(4).GetComponentInChildren<Text>().font = arialChina;

                CadenaMoviendo = "正在移动...";
                CadenaCopiando = "正在拷贝...";
                CadenaNoHasCopiado = "你没有剪切或复制要粘贴的任何内容";
                textoOptions.text = "OPTIONS\n说明";
                CadenaErrorCrearCarpeta = "您尝试创建的文件夹已存在";
                CadenaFtpActivo = "FTP已启用";
                CadenaRwActivo = "系统文件夹的完全R/W权限（危险，谨慎使用!）";
                CadenaHomeSalvado = "将此路径保存为“主页”(按R3键时返回这里)";
                CadenaNoHome = "无法到达";
                CadenaElemento = "要素";
                CadenaElementos = "要素";
                BotonCancelar.GetComponentInChildren<Text>().text = "取消";
                CadenaExtraer = "提取";
                CadenaEspere = "等待...";
                CadenaAvatarInstalado = "阿凡达已安装！";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "选择用户";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "好";
                CadenaErrorCrearFichero = "您嘗試創建的文件已存在";
                CadenaInstalar = "安装PKG";
                break;
            case 1: //SystemLanguage.Arabic:
                CadenaMoviendo = "ﻞﻘﻨﻟا ِرﺎﺟ";
                CadenaCopiando = "ﺦﺴﻨﻟا ِرﺎﺟ";
                CadenaNoHasCopiado = "ﻪﻘﺼﻟ ﻢﺘﻴﻟ ءﻲﺷ يأ ﺺﻗ وأ ﺦﺴﻨﺑ ﻢﻘﺗ ﻢﻟ";
                textoOptions.text = "OPTIONS\nتادﺎﺷرﻹا ﺢﺘﻔﻟ";
                CadenaErrorCrearCarpeta = "ﻢﺳﻻا اﺬﻬﺑ ﺪﻠﺠﻣ ﺪﺟﻮﻳ";
                CadenaFtpActivo = "تﺎﻔﻠﻤﻟا ﻞﻘﻧ لﻮﻛﻮﺗوﺮﺑ ﻞﻴﻌﻔﺗ ﻢﺗ";
                CadenaRwActivo = "(مﺎﻈﻨﻟا تﺎﻔﻠﻣ،!رﺬﺤﺑ فﺮﺼﺗ) ﺔﻤﻈﻧﻷا تﺎﻔﻠﻣ ﻲﻓ ﺔﺑﺎﺘﻛو ءاﺮﻗ تﺎﻧوذأ";
                CadenaHomeSalvado = "(ﺖﻗو يأ ﻲﻓ ﺎﻨﻫ ةدﻮﻌﻠﻟ ٣رأ ﻂﻐﺿإ) ''ﻲﺴﻴﺋر'' ـﻛ رﺎﺴﻤﻟا اﺬﻫ ﻆﻔﺤﻟا ﻢﺗ";
                CadenaNoHome = "لﻮﺻﻮﻟا ﻊﻴﻄﺘﺴﻳ ﻻ";
                CadenaElemento = "ﺮﺼﻨﻋ";
                CadenaElementos = "ﺮﺻﺎﻨﻋ";
                BotonCancelar.GetComponentInChildren<Text>().text = "ءﺎﻐﻟإ";
                CadenaExtraer = "جاﺮﺨﺘﺳا";
                CadenaEspere = "...ﺮﻈﺘﻧا";
                CadenaAvatarInstalado = "!ﺔﻳﺰﻣﺮﻟا ةرﻮﺼﻟا ﺖﻴﺒﺜﺗ ﻢﺗ";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "مﺪﺨﺘﺴﻤﻟا دﺪﺣ";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "ﺎﻨﺴﺣ";
                CadenaErrorCrearFichero = "ﻞﻌﻔﻟﺎﺑ دﻮﺟﻮﻣ هءﺎﺸﻧإ ﺖﻟوﺎﺣ يﺬﻟا ﻒﻠﻤﻟا";
                CadenaInstalar = UPersian.Utils.UPersianUtils.RtlFix("قم بتثبيت PKG");
                break;
            case 11: //SystemLanguage.Vietnamese:
                CadenaMoviendo = "Đang di chuyển";
                CadenaCopiando = "Đang sao chép";
                CadenaNoHasCopiado = "Bạn chưa cắt hoặc sao chép thứ gì";
                textoOptions.text = "OPTIONS\nChỉ dẫn";
                CadenaErrorCrearCarpeta = "Thư mục bạn tạo đã tồn tại";
                CadenaFtpActivo = "Địa chỉ IP của FTP";
                CadenaRwActivo = "Quyền R/W thư mục hệ thống đã được bật (Hãy thận trọng !)";
                CadenaHomeSalvado = "Đã lưu thư mục là trang chủ (ấn R3 để quay lại đây bất kỳ lúc nào)";
                CadenaNoHome = "Không thể đặt";
                CadenaElemento = "đối tượng";
                CadenaElementos = "đối tượng";
                BotonCancelar.GetComponentInChildren<Text>().text = "Hủy";
                CadenaExtraer = "Trích xuất";
                CadenaEspere = "Chờ đợi...";
                CadenaAvatarInstalado = "Avatar đã được cài đặt!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Chọn người dùng";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Đồng ý";
                CadenaErrorCrearFichero = "Tệp bạn cố tạo đã tồn tại";
                CadenaInstalar = "Cài đặt PKG";
                break;
            case 12: //Persian
                CadenaMoviendo = UPersian.Utils.UPersianUtils.RtlFix("در حال انتقال");
                CadenaCopiando = UPersian.Utils.UPersianUtils.RtlFix("در حال کپی");
                CadenaNoHasCopiado = UPersian.Utils.UPersianUtils.RtlFix("شما چیزی را برای کپی یا کات کردن انتخاب نکرده اید");
                textoOptions.text = "OPTIONS\n" + UPersian.Utils.UPersianUtils.RtlFix("برای دستورالعمل");
                CadenaErrorCrearCarpeta = UPersian.Utils.UPersianUtils.RtlFix("پوشه ای که در تلاش برای ساخت آن هستید، در حال حاضر وجود دارد");
                CadenaFtpActivo = "FTP " + UPersian.Utils.UPersianUtils.RtlFix("فعال است");
                CadenaRwActivo = UPersian.Utils.UPersianUtils.RtlFix("(دسترسی خواندن و نوشتن کامل در پوشه های سیستمی ‌‌(اخطار");
                CadenaHomeSalvado = UPersian.Utils.UPersianUtils.RtlFix("این مسیر را بعنوان خانه ذخیره کن");
                CadenaNoHome = UPersian.Utils.UPersianUtils.RtlFix("دسترسی به این مسیر فراهم نیست");
                CadenaElemento = UPersian.Utils.UPersianUtils.RtlFix("مورد");
                CadenaElementos = UPersian.Utils.UPersianUtils.RtlFix("مورد");
                BotonCancelar.GetComponentInChildren<Text>().text = UPersian.Utils.UPersianUtils.RtlFix("لغو");
                CadenaExtraer = UPersian.Utils.UPersianUtils.RtlFix("استخراج کردن");
                CadenaEspere = UPersian.Utils.UPersianUtils.RtlFix("...صبر کن");
                CadenaAvatarInstalado = UPersian.Utils.UPersianUtils.RtlFix("آواتار نصب شد!");
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = UPersian.Utils.UPersianUtils.RtlFix("کاربر را انتخاب کنید");
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = UPersian.Utils.UPersianUtils.RtlFix("خوب");
                CadenaErrorCrearFichero = UPersian.Utils.UPersianUtils.RtlFix("فایلی که سعی کردید ایجاد کنید از قبل وجود دارد");
                CadenaInstalar = UPersian.Utils.UPersianUtils.RtlFix("نصب PKG");
                break;
            case 13: //SystemLanguage.Russian:
                CadenaMoviendo = "Перемещение...";
                CadenaCopiando = "Копирование...";
                CadenaNoHasCopiado = "Вы ничего не выбрали для копирования или вставки";
                textoOptions.text = "OPTIONS\nдля инструкций";
                CadenaErrorCrearCarpeta = "Папка которую вы пытаетесь создать уже существует";
                CadenaFtpActivo = "FTP включён";
                CadenaRwActivo = "Полные права доступа к системным папкам (только для продвинутых пользователей !)";
                CadenaHomeSalvado = "Этот путь сохранен, как ''Домашний'' (Нажмите R3, чтобы вернуться сюда в любое время)";
                CadenaNoHome = "Не могу достигнуть";
                CadenaElemento = "элемент";
                CadenaElementos = "элементов";
                BotonCancelar.GetComponentInChildren<Text>().text = "Отмена";
                CadenaExtraer = "экстракт";
                CadenaEspere = "Подождите...";
                CadenaAvatarInstalado = "Аватар установлен!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Выберите пользователя";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Хорошо";
                CadenaErrorCrearFichero = "Файл, который вы пытались создать, уже существует.";
                CadenaInstalar = "Установить PKG";
                break;
            case 14: //SystemLanguage.Turkish:
                CadenaMoviendo = "Taşınıyor...";
                CadenaCopiando = "Kopyalanıyor...";
                CadenaNoHasCopiado = "Yapıştırmak için herhangi bir şey kopyalamadınız";
                textoOptions.text = "OPTIONS\naçıklamalar için";
                CadenaErrorCrearCarpeta = "Oluşturmaya çalıştığınız klasör zaten mevcut";
                CadenaFtpActivo = "FTP aktif";
                CadenaRwActivo = "Sistem klasöründe tam Okuma/Yazma yetkisi (Tehlikeli olabilir, dikkatli olun)";
                CadenaHomeSalvado = "Bu yolu ''Ana Sayfa'' olarak kaydet (herhangi bir zaman buraya dönmek için R3 'e bas)";
                CadenaNoHome = "Ulaşılamıyor";
                CadenaElemento = "parça";
                CadenaElementos = "parçalar";
                BotonCancelar.GetComponentInChildren<Text>().text = "İptal";
                CadenaExtraer = "Ayıkla";
                CadenaEspere = "Bekle...";
                CadenaAvatarInstalado = "Avatar yüklendi!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Kullanıcıyı seçin";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Tamam";
                CadenaErrorCrearFichero = "Oluşturmaya çalıştığınız dosya zaten var";
                CadenaInstalar = "PKG'yi yükleyin";
                break;
            case 15: //SystemLanguage.Polish:
                CadenaMoviendo = "Przeniesc...";
                CadenaCopiando = "Kopiowanie...";
                CadenaNoHasCopiado = "Nie wyciales ani nie skopiowales niczego do wklejenia";
                textoOptions.text = "OPTIONS\ninstrukcja obslugi";
                CadenaErrorCrearCarpeta = "Folder, ktory probujesz utworzyc, już istnieje";
                CadenaFtpActivo = "FTP jest Aktywne";
                CadenaRwActivo = "Pelne uprawnienia R/W w folderach systemowych (niebezpieczne, uważaj!)";
                CadenaHomeSalvado = "Zapisano te sciezke jako ''Home'' (nacisnij R3, aby wrocic tutaj w dowolnym momencie)";
                CadenaNoHome = "Nieosiagalny";
                CadenaElemento = "element";
                CadenaElementos = "elementy";
                BotonCancelar.GetComponentInChildren<Text>().text = "Odrzuć";
                CadenaExtraer = "Wyciąg";
                CadenaEspere = "Czekać...";
                CadenaAvatarInstalado = "Awatar zainstalowany!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Wybierz użytkownika";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Ok";
                CadenaErrorCrearFichero = "Plik, który próbujesz utworzyć, już istnieje";
                CadenaInstalar = "Zainstaluj PKG";
                break;
            case 16: //SystemLanguage.Swedish:
                CadenaMoviendo = "Flytta...";
                CadenaCopiando = "Kopiering...";
                CadenaNoHasCopiado = "Du har inte klippt eller kopierat något för att klistra in";
                textoOptions.text = "OPTIONS\nför instruktioner";
                CadenaErrorCrearCarpeta = "Den mapp du försökte skapa finns redan";
                CadenaFtpActivo = "FTP är aktiv";
                CadenaRwActivo = "Fullständiga R / W-tillstånd i systemmappar (farligt, var försiktig!)";
                CadenaHomeSalvado = "Sparade den här sökvägen som ''Hem'' (tryck R3 för att komma tillbaka hit när som helst)";
                CadenaNoHome = "Kan inte nå";
                CadenaElemento = "element";
                CadenaElementos = "elements";
                BotonCancelar.GetComponentInChildren<Text>().text = "Avbryt";
                CadenaExtraer = "Extrahera";
                CadenaEspere = "Vänta...";
                CadenaAvatarInstalado = "Avatar installerat!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Välj användaren";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Ok";
                CadenaErrorCrearFichero = "Filen du försökte skapa finns redan";
                CadenaInstalar = "Installera PKG";
                break;
            case 17: //SystemLanguage.Catalan:
                CadenaMoviendo = "En moviment...";
                CadenaCopiando = "Copiant...";
                CadenaNoHasCopiado = "No heu tallat ni copiat res per enganxar";
                textoOptions.text = "OPTIONS\nper obtenir instruccions";
                CadenaErrorCrearCarpeta = "a carpeta que vau intentar crear ja existeix";
                CadenaFtpActivo = "FTP està actiu";
                CadenaRwActivo = "Permisos RW complets a les carpetes de sistemes (perillós, aneu amb compte!)";
                CadenaHomeSalvado = "Directori guardar com Home (pulsa R3 per tornar aqui en qualsevol moment)";
                CadenaNoHome = "No es pot arribar";
                CadenaElemento = "element";
                CadenaElementos = "elements";
                BotonCancelar.GetComponentInChildren<Text>().text = "Cancela";
                CadenaExtraer = "Extracte";
                CadenaEspere = "Espereu...";
                CadenaAvatarInstalado = "Avatar instal·lat!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Seleccioneu l’usuari";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "D'acord";
                CadenaErrorCrearFichero = "El fitxer que heu intentat crear ja existeix";
                CadenaInstalar = "Instal-leu PKG";
                break;
            case 18: //SystemLanguage.Danish:
                CadenaMoviendo = "Flytter...";
                CadenaCopiando = "Kopiér...";
                CadenaNoHasCopiado = "Du har ikke klippet eller kopieret noget for at indsætte";
                textoOptions.text = "OPTIONS\nfor instruktioner";
                CadenaErrorCrearCarpeta = "Den mappe, du forsøgte at oprette, eksisterer allerede";
                CadenaFtpActivo = "FTP er aktiv";
                CadenaRwActivo = "Fuld R/W tilladelse i systemmapper (Farlig, pas på!)";
                CadenaHomeSalvado = "Gemt denne sti som ''Hjem'' (tryk R3 for at komme tilbage hertil til enhver tid)";
                CadenaNoHome = "Kan ikke nå";
                CadenaElemento = "element";
                CadenaElementos = "elementer";
                BotonCancelar.GetComponentInChildren<Text>().text = "Annuller";
                CadenaExtraer = "Uddrag";
                CadenaEspere = "Vente...";
                CadenaAvatarInstalado = "Avatar installeret!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Vælg bruger";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Okay";
                CadenaErrorCrearFichero = "Den fil, du forsøgte at oprette, findes allerede";
                CadenaInstalar = "Installer PKG";
                break;
            case 19: //SystemLanguage.Greek:
                CadenaMoviendo = "Μεταφέροντας...";
                CadenaCopiando = "Αντιγράφοντας...";
                CadenaNoHasCopiado = "Δεν έχεις αποκόψει ή αντιγράψει κάτι για επικόλληση";
                textoOptions.text = "OPTIONS\nγια οδηγίες";
                CadenaErrorCrearCarpeta = "Ο φάκελος που προσπάθησες να δημιουργήσεις, υπάρχει ήδη";
                CadenaFtpActivo = "Το FTP είναι ενεργοποιημένο";
                CadenaRwActivo = "Πλήρης R/W άδεια στους φακέλους των συστημάτων (Κίνδυνος, με προσοχή!)";
                CadenaHomeSalvado = "Η διαδρομή αποθηκεύτηκε ως ''Αρχική'' (πάτησε R3 για να επιστρέφεις εδώ κάθε φορά)";
                CadenaNoHome = "Δεν εντοπίζεται";
                CadenaElemento = "στοιχείο";
                CadenaElementos = "στοιχεία";
                BotonCancelar.GetComponentInChildren<Text>().text = "Ακύρωση";
                CadenaExtraer = "Εκχύλισμα";
                CadenaEspere = "Περίμενε...";
                CadenaAvatarInstalado = "Εγκαταστάθηκε το avatar!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Επιλέξτε τον χρήστη";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Εντάξει";
                CadenaErrorCrearFichero = "Το αρχείο που προσπαθήσατε να δημιουργήσετε υπάρχει ήδη";
                CadenaInstalar = "Εγκαταστήστε το PKG";
                break;
            case 20: //SystemLanguage.Indonesian:
                CadenaMoviendo = "Sedang memindahkan...";
                CadenaCopiando = "Sedang menyalin...";
                CadenaNoHasCopiado = "Anda belum memotong atau menyalin sesuatu pun untuk ditempel";
                textoOptions.text = "OPTIONS\npetunjuk";
                CadenaErrorCrearCarpeta = "Map yang akan dibuat sudah ada";
                CadenaFtpActivo = "FTP aktif";
                CadenaRwActivo = "Izin Baca/Tulis penuh dalam map sistem (Berbahaya, hati-hati !)";
                CadenaHomeSalvado = "Menyimpan jalur ini sebagai ''Beranda'' (tekan R3 untuk kembali ke sini kapan saja)";
                CadenaNoHome = "Tidak dapat menjangkau";
                CadenaElemento = "elemen";
                CadenaElementos = "elemen";
                BotonCancelar.GetComponentInChildren<Text>().text = "Membatalkan";
                CadenaExtraer = "Ekstract";
                CadenaEspere = "Tunggu...";
                CadenaAvatarInstalado = "Avatar terpasang!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Pilih pengguna";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Baik";
                CadenaErrorCrearFichero = "File yang Anda coba buat sudah ada";
                CadenaInstalar = "Instal PKG";
                break;
            case 21: //SystemLanguage.Dutch: Holandes
                CadenaMoviendo = "Aan het verplaatsen";
                CadenaCopiando = "Aan het kopiëren";
                CadenaNoHasCopiado = "U hebt niets geknipt of gekopieerd om te plakken";
                textoOptions.text = "OPTIONS\nvoor instructies";
                CadenaErrorCrearCarpeta = "De map die u probeert aan te maken bestaat al";
                CadenaFtpActivo = "FTP is actief";
                CadenaRwActivo = "Volledige R/W machtigingen in systeemmappen (gevaarlijk, wees voorzichtig!)";
                CadenaHomeSalvado = "Dit pad is nu bewaard als ''Home'' (Gebruik R3 om naar hier terug te keren)";
                CadenaNoHome = "Kan de volgende locatie niet bereiken";
                CadenaElemento = "element";
                CadenaElementos = "elementen";
                BotonCancelar.GetComponentInChildren<Text>().text = "Annuleren";
                CadenaExtraer = "Extract";
                CadenaEspere = "Waiht...";
                CadenaAvatarInstalado = "Avatar geïnstalleerd!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "Selecteer de gebruiker";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "Ok";
                CadenaErrorCrearFichero = "Het bestand dat u probeerde te maken, bestaat al";
                CadenaInstalar = "Installeer PKG";
                break;
            case 22: //SystemLanguage.Korean:
                txtCamino.font = arialKorea;
                txtCantidad.font = arialKorea;
                txtLog.font = arialKorea;
                textoOptions.font = arialKorea;
                PanelCopiando.GetComponentInChildren<Text>().font = arialKorea;
                BotonCancelar.GetComponentInChildren<Text>().font = arialKorea;
                PanelDescompactador.transform.GetChild(1).GetComponentInChildren<Text>().font = arialKorea;
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().font = arialKorea;
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().font = arialKorea;
                PanelPKG.transform.GetChild(4).GetComponentInChildren<Text>().font = arialKorea;

                CadenaMoviendo = "이동 중입니다...";
                CadenaCopiando = "복사 중입니다...";
                CadenaNoHasCopiado = "붙여넣을 파일이 없습니다";
                textoOptions.text = "OPTIONS\n버튼을 누르면 도움말";
                CadenaErrorCrearCarpeta = "폴더가 이미 생성되어 있습니다";
                CadenaFtpActivo = "FTP가 활성화 되었습니다";
                CadenaRwActivo = "시스템 폴더의 모든 읽고 쓰는 권한 부여 (시스템이 망가질 수 있으니 조심해서 사용해 주세요!)";
                CadenaHomeSalvado = "이 경로를 ''홈''으로 설정 (R3를 누르면 이 경로로 이동합니다)";
                CadenaNoHome = "찾을 수 없습니다";
                CadenaElemento = "개 항목";
                CadenaElementos = "개 항목";
                BotonCancelar.GetComponentInChildren<Text>().text = "취소";
                CadenaExtraer = "추출";
                CadenaEspere = "잠시만 기다려주세요...";
                CadenaAvatarInstalado = "아바타가 설치되었습니다!";
                PanelSelecionUsuario.transform.GetChild(0).GetComponentInChildren<Text>().text = "사용자 프로필을 선택";
                PanelSelecionUsuario.transform.GetChild(1).GetComponent<Text>().text = "확인";
                CadenaErrorCrearFichero = "생성하려는 파일이 이미 존재합니다.";
                CadenaInstalar = "PKG 설치";
                break;
        }
        PanelDescompactador.transform.GetChild(1).GetComponentInChildren<Text>().text = CadenaExtraer;
        PanelPKG.transform.GetChild(4).GetComponentInChildren<Text>().text = CadenaInstalar;

        try
        {
            //if (FW > 0)
            {

                FreeUnjail(get_firmware());
                //FreeUnjail(FW);
            }
        }
        catch {; }

        CrearDirectorio();

        if (File.Exists("/data/F.txt"))
        {
            StartCoroutine(LeerTemperatura(false));
        }
        else
        {
            StartCoroutine(LeerTemperatura(true));
        }

        //PlayerPrefs.SetInt("Idioma", 0);
        //PlayerPrefs.SetInt("FW755", 0);
        //PlayerPrefs.Save();
    }

    IEnumerator LeerTemperatura(bool EnGradosC)
    {
        while (true)
        {
            try
            {
                if (EnGradosC)
                {
                    txtTemperatura.text = Temperature().ToString() + " ºC";
                }
                else
                {
                    txtTemperatura.text = (((float)Temperature() * 9 / 5) + 32).ToString("n1") + " ºF";
                }
            }
            catch {; }

            yield return new WaitForSeconds(3);
        }
    }

    void CrearDirectorio()
    {
        txtCamino.text = camino.Replace("\\", "/");

        List<string> CarpetasCreadas = new List<string>();
        List<string> FicherosCreados = new List<string>();
        GameObject objeto = null;

        scaneo = Directory.GetFileSystemEntries(txtCamino.text);
        foreach (string registro in scaneo)
        {
            if (Directory.Exists(registro))
            {
                CarpetasCreadas.Add(registro);
            }
            else
            {
                FicherosCreados.Add(registro);
            }
        }

        for (int i = 0; i < CarpetasCreadas.Count; i++)
        {
            switch (CarpetasCreadas[i])
            {
                case "/dev":
                    objeto = Instantiate(carpetasPrefabBlackList, transform);
                    break;
                case "/mnt":
                case "/mnt/usb0":
                case "/mnt/usb1":
                case "/mnt/usb2":
                case "/mnt/usb3":
                case "/mnt/usb4":
                case "/mnt/usb5":
                case "/mnt/usb6":
                case "/mnt/usb7":
                    objeto = Instantiate(carpetasPrefabEspecial, transform);
                    break;
                default:
                    objeto = Instantiate(carpetasPrefab, transform);
                    break;
            }

            objeto.GetComponentInChildren<Text>().text = Path.GetFileName(CarpetasCreadas[i]);
            objeto.transform.GetChild(3).GetComponent<Text>().text = FechaCreada(CarpetasCreadas[i]);

            ObjetosCreados.Add(objeto);
            TODOS.Add(CarpetasCreadas[i]);
        }
        for (int i = 0; i < FicherosCreados.Count; i++)
        {
            switch (Path.GetExtension(FicherosCreados[i]).ToLower())
            {
                case ".ogg":
                case ".wav":
                    objeto = Instantiate(ficherosPrefabMP3, transform);
                    break;
                case ".mp4":
                case ".mov":
                    objeto = Instantiate(ficherosPrefabMP4, transform);
                    break;
                case ".jpg":
                case ".png":
                case ".dds":
                    objeto = Instantiate(ficherosPrefabFOT, transform);
                    break;
                case ".txt":
                case ".ini":
                case ".bat":
                case ".xml":
                case ".json":
                case ".cfg":
                    objeto = Instantiate(ficherosPrefabTXT, transform);
                    break;
                case ".sfo":
                    objeto = Instantiate(ficherosPrefabSFO, transform);
                    break;
                case ".pkg":
                    objeto = Instantiate(ficherosPrefabPKG, transform);
                    break;
                case ".zip":
                case ".rar":
                case ".tar":
                    objeto = Instantiate(ficherosPrefabRAR, transform);
                    break;
                case ".xtheme":
                    objeto = Instantiate(ficherosPrefabTHEME, transform);
                    break;
                case ".xavatar":
                    objeto = Instantiate(ficherosPrefabAVATAR, transform);
                    break;
                //case ".pup":
                //    objeto = Instantiate(ficherosPrefabPUP, transform);
                //    break;
                default:
                    objeto = Instantiate(ficherosPrefab, transform);
                    break;
            }

            objeto.GetComponentInChildren<Text>().text = Path.GetFileName(FicherosCreados[i]);
            objeto.transform.GetChild(3).GetComponent<Text>().text = Tamaño(FicherosCreados[i]);

            ObjetosCreados.Add(objeto);
            TODOS.Add(FicherosCreados[i]);
        }

        scrollRect.verticalNormalizedPosition = 1;

        // si hay algo selecionar el 1ro de la lista
        if (ObjetosCreados.Count > 0)
        {
            ObjetosCreados[0].transform.GetChild(0).gameObject.SetActive(true);
            camino = TODOS[0];
        }
        else
        {
            camino = "";
        }

        if (ObjetosCreados.Count == 1)
            txtCantidad.text = "1 " + CadenaElemento;
        else
            txtCantidad.text = ObjetosCreados.Count.ToString() + " " + CadenaElementos;

        Paso = true;
        ObjetosSelecionados.Clear();
        Multiseleccion = false;
    }

    private string FechaCreada(string carpeta)
    {
        DirectoryInfo folderData = new DirectoryInfo(carpeta);
        return folderData.CreationTime.ToString("yyyy/MM/dd hh:mm tt").ToLower();
    }

    private string Tamaño(string fichero)
    {
        FileInfo fileData = new FileInfo(fichero);
        float KB = fileData.Length / 1024f;
        string sufijo = " KB";

        if (KB >= 1024)
        {
            KB = KB / 1024f;
            sufijo = " MB";
        }

        if (KB >= 1024)
        {
            KB = KB / 1024f;
            sufijo = " GB";
        }

        KB = Mathf.Round(KB * 10) / 10;
        return KB.ToString() + sufijo + " - " + fileData.LastWriteTime.ToString("yyyy/MM/dd hh:mm tt").ToLower();
    }

    void Update()
    {
        if (EnEditorTexto)
        {
            if (PanelEditorTexto1.activeInHierarchy)
            {
                if ((Input.GetAxis("dpad1_vertical") > 0 || Input.GetAxis("leftstick1vertical") < 0 || Input.GetAxis("rightstick1vertical") < 0 || Input.GetKey(KeyCode.UpArrow)) && PanelEditorTexto1.GetComponentInChildren<Scrollbar>().value < 1 && Paso)
                {
                    Paso = false;
                    PanelEditorTexto1.GetComponentInChildren<Scrollbar>().value += 0.05f;
                    StartCoroutine(SeguirPasando());
                }

                if ((Input.GetAxis("dpad1_vertical") < 0 || Input.GetAxis("leftstick1vertical") > 0 || Input.GetAxis("rightstick1vertical") > 0 || Input.GetKey(KeyCode.DownArrow)) && PanelEditorTexto1.GetComponentInChildren<Scrollbar>().value > 0 && Paso)
                {
                    Paso = false;
                    PanelEditorTexto1.GetComponentInChildren<Scrollbar>().value -= 0.05f;
                    StartCoroutine(SeguirPasando());
                }
            }
            else if (PanelDescompactador.activeInHierarchy)
            {
                if ((Input.GetAxis("dpad1_vertical") > 0 || Input.GetAxis("leftstick1vertical") < 0 || Input.GetAxis("rightstick1vertical") < 0 || Input.GetKey(KeyCode.UpArrow)) && PanelDescompactador.GetComponentInChildren<Scrollbar>().value < 1 && Paso)
                {
                    Paso = false;
                    PanelDescompactador.GetComponentInChildren<Scrollbar>().value += 0.05f;
                    StartCoroutine(SeguirPasando());
                }

                if ((Input.GetAxis("dpad1_vertical") < 0 || Input.GetAxis("leftstick1vertical") > 0 || Input.GetAxis("rightstick1vertical") > 0 || Input.GetKey(KeyCode.DownArrow)) && PanelDescompactador.GetComponentInChildren<Scrollbar>().value > 0 && Paso)
                {
                    Paso = false;
                    PanelDescompactador.GetComponentInChildren<Scrollbar>().value -= 0.05f;
                    StartCoroutine(SeguirPasando());
                }
            }
        }

        if (!EnOpciones && !EnImagen && !Pegando && !EnEditorTexto && !EnTeclado)
        {
            // multiselecion
            if (Input.GetKeyDown(KeyCode.Joystick1Button5) || Input.GetKeyDown(KeyCode.M))
            {
                // selecionar todos si RT esta presionado...
                if (Input.GetAxis("joystick1_right_trigger") != 0)
                {
                    Multiseleccion = true;
                    MultiseleccionCopiada = false;
                    MultiseleccionCortada = false;
                    ObjetosSelecionados.Clear();
                    ObjetosCaminos.Clear();

                    for (int i = 0; i < TODOS.Count; i++)
                    {
                        ObjetosSelecionados.Add(i);
                        ObjetosCaminos.Add(TODOS[i]);
                        ObjetosCreados[i].transform.GetChild(1).gameObject.SetActive(true);
                    }

                    return;
                }

                if (!Multiseleccion)
                    ObjetosCaminos.Clear();

                Multiseleccion = true;
                bool ok = true;
                for (int i = 0; i < ObjetosSelecionados.Count; i++)
                {
                    if (ObjetosSelecionados[i] == Posicion)
                    {
                        if (MultiseleccionCopiada || MultiseleccionCortada)
                        {
                            MultiseleccionCopiada = false;
                            MultiseleccionCortada = false;
                            ObjetosSelecionados.Clear();
                        }

                        ObjetosSelecionados.RemoveAt(i);
                        ObjetosCaminos.RemoveAt(i);
                        ObjetosCreados[Posicion].transform.GetChild(1).gameObject.SetActive(false);
                        ok = false;
                    }
                }

                if (ok == true)
                {
                    if (MultiseleccionCopiada || MultiseleccionCortada)
                    {
                        MultiseleccionCopiada = false;
                        MultiseleccionCortada = false;
                        ObjetosSelecionados.Clear();
                    }

                    ObjetosSelecionados.Add(Posicion);
                    ObjetosCaminos.Add(camino);
                    ObjetosCreados[Posicion].transform.GetChild(1).gameObject.SetActive(true);
                }
                if (ObjetosSelecionados.Count == 0)
                {
                    Multiseleccion = false;
                }
            }

            // cancelar multiselecion
            if (Input.GetKeyDown(KeyCode.Joystick1Button4) || Input.GetKeyDown(KeyCode.C))
            {
                ObjetosSelecionados.Clear();
                ObjetosCaminos.Clear();
                Multiseleccion = false;
                MultiseleccionCopiada = false;
                MultiseleccionCortada = false;

                for (int i = 0; i < scaneo.Length; i++)
                {
                    ObjetosCreados[i].transform.GetChild(1).gameObject.SetActive(false);
                }
            }

            // movimientos arriba y abajo
            if ((Input.GetAxis("dpad1_vertical") > 0 || Input.GetAxis("leftstick1vertical") < 0 || Input.GetAxis("rightstick1vertical") < 0 || Input.GetKey(KeyCode.UpArrow)) && Posicion > 0 && Paso)
            {
                Paso = false;
                AplicarTema = false;

                ObjetosCreados[Posicion].transform.GetChild(0).gameObject.SetActive(false);
                Posicion--;
                ObjetosCreados[Posicion].transform.GetChild(0).gameObject.SetActive(true);
                camino = TODOS[Posicion];

                if (contentPanel.anchoredPosition.y > 0)
                {
                    contentPanel.anchoredPosition -= new Vector2(0, 69);
                }
                if (contentPanel.anchoredPosition.y < 0)
                {
                    contentPanel.anchoredPosition = new Vector2(0, 0);
                }

                StartCoroutine(SeguirPasando());
            }

            if ((Input.GetAxis("dpad1_vertical") < 0 || Input.GetAxis("leftstick1vertical") > 0 || Input.GetAxis("rightstick1vertical") > 0 || Input.GetKey(KeyCode.DownArrow)) && Posicion < scaneo.Length - 1 && Paso)
            {
                Paso = false;
                AplicarTema = false;

                ObjetosCreados[Posicion].transform.GetChild(0).gameObject.SetActive(false);
                Posicion++;
                ObjetosCreados[Posicion].transform.GetChild(0).gameObject.SetActive(true);
                camino = TODOS[Posicion];

                if (scrollRect.verticalNormalizedPosition >= 0 && Posicion > 9)
                {
                    contentPanel.anchoredPosition += new Vector2(0, 69);
                }

                StartCoroutine(SeguirPasando());
            }

            // accesos directos a los USB con el DPad <- , ->
            if ((Input.GetAxis("dpad1_horizontal") < 0 || Input.GetKeyDown(KeyCode.LeftArrow)) && Paso)
            {
                Paso = false;

                Nivel = 2;
                UltimaPos.Clear();
                UltimaPos.Add(new Vector2(0, 0));
                UltimaPos.Add(new Vector2(0, 0));

                LOG = "";
                LimpiarTodo();
                camino = "/mnt/usb0";
                CrearDirectorio();

                StartCoroutine(SeguirPasando());
            }

            if ((Input.GetAxis("dpad1_horizontal") > 0 || Input.GetKeyDown(KeyCode.RightArrow)) && Paso)
            {
                Paso = false;

                Nivel = 2;
                UltimaPos.Clear();
                UltimaPos.Add(new Vector2(0, 0));
                UltimaPos.Add(new Vector2(0, 0));

                LOG = "";
                LimpiarTodo();
                camino = "/mnt/usb1";
                CrearDirectorio();

                StartCoroutine(SeguirPasando());
            }

            // ir a Home R3
            if ((Input.GetKeyDown(KeyCode.Joystick1Button9) || Input.GetKeyDown(KeyCode.H)) && Paso)
            {
                Paso = false;

                string Home = PlayerPrefs.GetString("Home", "/");
                if (Directory.Exists(Home))
                {
                    Nivel = PlayerPrefs.GetInt("Nivel", 1);
                    UltimaPos.Clear();
                    for (int i = 0; i < Nivel; i++)
                    {
                        UltimaPos.Add(new Vector2(0, 0));
                    }

                    LOG = "";
                    LimpiarTodo();

                    camino = Home;
                    CrearDirectorio();
                }
                else
                {
                    LOG = CadenaNoHome + " ''" + Home + "''";
                }

                StartCoroutine(SeguirPasando());
            }

            // Acceso directo al FTP con L3
            if ((Input.GetKeyDown(KeyCode.Joystick1Button8) || Input.GetKeyDown(KeyCode.F)) && Paso)
            {
                Paso = false;

                ActivarFTP();

                StartCoroutine(SeguirPasando());
            }

            // abrir o ejecutar
            if ((Input.GetKeyDown(KeyCode.Joystick1Button0) || Input.GetKeyDown(KeyCode.Keypad2)) && Paso)
            {
                Paso = false;

                try
                {


                    LOG = "";
                    if (Directory.Exists(camino)) // si es una carpeta abrirla
                    {
                        Nivel++;
                        UltimaPos.Add(new Vector2(Posicion, contentPanel.anchoredPosition.y));

                        LimpiarTodo();
                        CrearDirectorio();
                    }
                    else // si no ejecutar el fichero si esta soportado
                    {
                        switch (Path.GetExtension(camino).ToLower())
                        {
                            case ".ogg":
                            case ".wav":
                                PlayAudio();
                                break;
                            case ".mp4":
                            case ".mov":
                                PlayVideo();
                                break;
                            case ".jpg":
                            case ".png":
                                MostrarImagen();
                                break;
                            case ".dds":
                                MostrarImagenDDS();
                                break;
                            case ".txt":
                            case ".ini":
                            case ".bat":
                            case ".xml":
                            case ".json":
                            case ".cfg":
                                MostrarTexto();
                                break;
                            case ".sfo":
                                MostrarSFO();
                                break;
                            case ".pkg":
                                MostrarPKG();
                                break;
                            case ".zip":
                            case ".rar":
                            case ".tar":
                                Descompactar(Path.GetExtension(camino).ToLower());
                                break;
                            case ".xtheme":
                                if (!AplicarTema)
                                {
                                    AplicarTema = true;
                                    StartCoroutine(NoAplicarTema());
                                }
                                else
                                {
                                    AplicarTema = false;
                                    DescompactarTema(camino);
                                }
                                break;
                            case ".xavatar":
                                if (!AplicarAvatar)
                                {
                                    AplicarAvatar = true;
                                    StartCoroutine(NoAplicarAvatar());
                                }
                                else
                                {
                                    AplicarAvatar = false;
                                    DescompactarAvatar(camino, "");
                                }
                                break;
                                //case ".pup":
                                //    try
                                //    {
                                //        LOG = Marshal.PtrToStringAnsi(PupDecrypt(camino));
                                //    }
                                //    catch (System.Exception ex)
                                //    {
                                //        LOG = "Error " + ex.Message;
                                //        SistemaSonidos.instancia.PlayError();
                                //    }
                                //    break;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    LOG = "Error " + ex.Message;
                    SistemaSonidos.instancia.PlayError();
                }

                StartCoroutine(SeguirPasando());
            }

            // Instrucciones
            if (Input.GetKeyDown(KeyCode.Joystick1Button7) || Input.GetKeyDown(KeyCode.O))
            {
                EnImagen = true;

                PanelImagenImagen.transform.rotation = new Quaternion(0, 0, 0, 0);
                PanelImagenImagen.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1300, 900);
                PanelImagen.gameObject.SetActive(true);
                PanelImagenImagen.sprite = instrucciones;
            }
        }
        else if (EnImagen)
        {
            if ((Input.GetKeyDown(KeyCode.Joystick1Button0) || Input.GetKeyDown(KeyCode.Keypad2)))
            {
                if (PanelPKG.activeSelf == true)
                {
                    try
                    {
                        //pkg item
                        var pkginfo = PS4_Tools.PKG.SceneRelated.Read_PKG(camino);
                        
                        try
                        {
                            if (!Directory.Exists("/data/"))
                            {
                                Directory.CreateDirectory("/data/");
                            }
                            if (File.Exists("/data/" + pkginfo.Content_ID + ".png"))
                            {
                                File.Delete("/data/" + pkginfo.Content_ID + ".png");
                            }
                            if (pkginfo.Icon != null)
                            {
                                File.WriteAllBytes("/data/" + pkginfo.Content_ID + ".png", pkginfo.Icon);
                            }
                        }
                        catch (Exception ex)
                        {
                            // SendMessageToPS4(ex.Message);
                        }
                        string imagepath = "";

                        try
                        {
                            if (File.Exists("/data/" + pkginfo.Content_ID + ".png"))
                            {
                                imagepath = "/data/" + pkginfo.Content_ID + ".png";
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                        if(imagepath == "")
                        {
                            imagepath = "https://i.imgur.com/J2i4yq1.png";
                        }
                        if (Application.platform == RuntimePlatform.PS4)
                        {
                            //SendMessageToPS4("Location:" + camino);
                            //SendMessageToPS4("PS4_Title:" + pkginfo.PS4_Title);
                            //SendMessageToPS4("imagepath:" + imagepath);
                            InstallPKG(camino, pkginfo.PS4_Title, imagepath);
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        // mostrar opciones
        if (Input.GetKeyDown(KeyCode.Joystick1Button3) || Input.GetKeyDown(KeyCode.Keypad8))
        {
            if (Input.GetAxis("joystick1_left_trigger") == 0) // normales
            {
                if (!EnImagen && !Pegando && !EnEditorTexto && !EnTeclado && !PanelOpcionesAvanzadas.gameObject.activeInHierarchy)
                {
                    if (camino.IndexOf("/dev") != 0)
                    {
                        LOG = "";
                        EnOpciones = !EnOpciones;
                        PanelOpciones.SetActive(EnOpciones);
                    }
                }
            }
            else // avanzadas
            {
                if (FW > 0)
                {
                    if (!EnImagen && !Pegando && !EnEditorTexto && !EnTeclado && !PanelOpciones.gameObject.activeInHierarchy)
                    {
                        LOG = "";
                        EnOpciones = !EnOpciones;
                        PanelOpcionesAvanzadas.SetActive(EnOpciones);
                    }
                }
            }
        }

        // stop musica, cancelar pegado o extraer compactado
        if (Input.GetKeyDown(KeyCode.Joystick1Button2) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            if (!EnTeclado && !Pegando && !EnEditorTexto)
            {
                LOG = "";
                miAudioSource.Stop();
            }

            // Cancelar cuando se esta pegando
            if (Pegando)
            {
                CancelarPegado = true;
            }

            // Extraer si es un compactado
            if (EnEditorTexto && PanelDescompactador.activeInHierarchy && !Extrayendo)
            {
                Extrayendo = true;
                StartCoroutine(DescompactarAccion(Path.GetExtension(camino).ToLower()));
            }
        }

        // atras o cerrar opciones
        if (Input.GetKeyDown(KeyCode.Joystick1Button1) || Input.GetKeyDown(KeyCode.Keypad6))
        {
            if (EnOpciones)
            {
                LOG = "";
                EnOpciones = false;
                PanelOpciones.SetActive(false);
                PanelOpcionesAvanzadas.SetActive(false);
                return;
            }

            if (EnImagen)
            {
                LOG = "";
                EnImagen = false;
                PanelImagen.gameObject.SetActive(false);
                PanelPKG.gameObject.SetActive(false);
                PanelVideo.gameObject.GetComponentInChildren<VideoPlayer>().Stop();
                PanelVideo.gameObject.SetActive(false);
                return;
            }

            if (EnEditorTexto)
            {
                if (!PanelEditorTexto2.activeInHierarchy)
                {
                    LOG = "";
                    EnEditorTexto = false;
                    PanelEditorTexto1.gameObject.SetActive(false);
                    PanelDescompactador.gameObject.SetActive(false);
                    PanelSelecionUsuario.gameObject.SetActive(false);
                    txtCantidad.gameObject.SetActive(true);
                }

                return;
            }

            if (EnTeclado)
            {
                LOG = "";
                EnTeclado = false;
                PanelTeclado.gameObject.SetActive(false);
                return;
            }

            if (!Pegando)
            {
                if (txtCamino.text != "/")
                {
                    Nivel--;

                    LOG = "";
                    LimpiarTodo();

                    camino = txtCamino.text.Substring(0, txtCamino.text.LastIndexOf(_s_));
                    if (camino.Length <= 1) // para PS4 cambiar por 1, para Win 2
                    {
                        camino += _s_;
                    }

                    CrearDirectorio();

                    Posicion = (int)UltimaPos[Nivel].x;
                    contentPanel.anchoredPosition += new Vector2(0, UltimaPos[Nivel].y);
                    UltimaPos.RemoveAt(Nivel);

                    try
                    {
                        ObjetosCreados[0].transform.GetChild(0).gameObject.SetActive(false);
                        ObjetosCreados[Posicion].transform.GetChild(0).gameObject.SetActive(true);
                        camino = TODOS[Posicion];
                    }
                    catch
                    {
                        Posicion = 0;
                        contentPanel.anchoredPosition = new Vector2(0, 0);
                        ObjetosCreados[0].transform.GetChild(0).gameObject.SetActive(true);
                        camino = TODOS[0];
                    }
                }
            }
        }

        // progressbar para cuando este pegando
        if (PanelCopiando.gameObject.activeInHierarchy)
        {
            if (CantArchivos <= 0)
            {
                PanelCopiando.GetComponentInChildren<Slider>().value = 0;
                PanelCopiando.gameObject.SetActive(false);
                Pegando = false;

                if (AccionSobrescribir == Sobreescribir.Stop || AccionSobrescribir == Sobreescribir.Si || AccionSobrescribir == Sobreescribir.SiTodo)
                {
                    SistemaSonidos.instancia.PlayFinalizoCopia();
                }

                AccionSobrescribir = Sobreescribir.Stop;
            }
            else
            {
                PanelCopiando.GetComponentInChildren<Slider>().value = PanelCopiando.GetComponentInChildren<Slider>().maxValue - CantArchivos;
            }
        }

        txtLog.text = LOG;
    }

    private void LimpiarTodo()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        ObjetosCreados.Clear();
        TODOS.Clear();
        Posicion = 0;
    }

    private IEnumerator SeguirPasando()
    {
        if (Input.GetAxis("joystick1_left_trigger") != 0)
        {
            yield return null;
        }
        else
        {
            yield return new WaitForSeconds(0.15f);
        }

        Paso = true;
    }

    private IEnumerator NoAplicarTema()
    {
        yield return new WaitForSeconds(0.5f);
        AplicarTema = false;
    }

    private IEnumerator NoAplicarAvatar()
    {
        yield return new WaitForSeconds(0.5f);
        AplicarAvatar = false;
    }

    private void MostrarImagen()
    {
        EnImagen = true;

        byte[] bytes = File.ReadAllBytes(camino);
        Texture2D texture = new Texture2D(0, 0, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Trilinear;
        texture.LoadImage(bytes);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.0f, 0.0f), 1.0f);

        float AR = 0;
        float XX = 0;
        float YY = 0;
        if (texture.height >= 1080)
        {
            AR = (float)texture.width / (float)texture.height;
            YY = Mathf.Min(texture.width, 1080);
            XX = YY * AR;

            if (XX > 1920)
            {
                XX = 1920;
                YY = 1920 / AR;
            }
        }
        else
        {
            AR = (float)texture.width / (float)texture.height;
            XX = Mathf.Min(texture.width, 1920);
            YY = XX / AR;
        }

        PanelImagenImagen.transform.rotation = new Quaternion(0, 0, 0, 0);
        PanelImagenImagen.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(XX, Mathf.RoundToInt(YY));
        PanelImagen.gameObject.SetActive(true);
        PanelImagenImagen.sprite = sprite;
    }

    public static Texture2D LoadTextureDXT(byte[] ddsBytes, TextureFormat textureFormat)
    {
        byte ddsSizeCheck = ddsBytes[4];
        if (ddsSizeCheck != 124)
            return null;

        int height = ddsBytes[13] * 256 + ddsBytes[12];
        int width = ddsBytes[17] * 256 + ddsBytes[16];

        int DDS_HEADER_SIZE = 128;
        byte[] dxtBytes = new byte[ddsBytes.Length - DDS_HEADER_SIZE];
        System.Buffer.BlockCopy(ddsBytes, DDS_HEADER_SIZE, dxtBytes, 0, ddsBytes.Length - DDS_HEADER_SIZE);

        Texture2D texture = new Texture2D(width, height, textureFormat, false);
        texture.LoadRawTextureData(dxtBytes);
        texture.Apply(false);

        return (texture);
    }

    private void MostrarImagenDDS()
    {
        EnImagen = true;

        try
        {
            byte[] bytes = File.ReadAllBytes(camino);
            Texture2D texture = LoadTextureDXT(bytes, TextureFormat.DXT1);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.0f, 0.0f), 1.0f);

            float AR = 0;
            float XX = 0;
            float YY = 0;
            if (texture.height >= 1080)
            {
                AR = (float)texture.width / (float)texture.height;
                YY = Mathf.Min(texture.width, 1080);
                XX = YY * AR;

                if (XX > 1920)
                {
                    XX = 1920;
                    YY = 1920 / AR;
                }
            }
            else
            {
                AR = (float)texture.width / (float)texture.height;
                XX = Mathf.Min(texture.width, 1920);
                YY = XX / AR;
            }

            PanelImagenImagen.transform.rotation = new Quaternion(-180, 0, 0, 0);
            PanelImagenImagen.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(XX, Mathf.RoundToInt(YY));
            PanelImagen.gameObject.SetActive(true);
            PanelImagenImagen.sprite = sprite;
        }
        catch (System.Exception ex)
        {
            LOG = "Error " + ex.Message;
            SistemaSonidos.instancia.PlayError();
        }
    }

    private void PlayAudio()
    {
        StartCoroutine(loadFile(camino));
    }

    IEnumerator loadFile(string path)
    {
        WWW www = new WWW("file://" + path);

        AudioClip myAudioClip = www.GetAudioClip();
        while (myAudioClip.loadState != AudioDataLoadState.Loaded)
            yield return www;

        miAudioSource.clip = myAudioClip;
        miAudioSource.Play();
    }

    private void PlayVideo()
    {
        EnImagen = true;
        EnVideo = true;
        PanelVideo.gameObject.SetActive(true);
        PanelVideo.gameObject.GetComponentInChildren<VideoPlayer>().Stop();

        try
        {
            StopCoroutine(loadVideo(camino));
            StartCoroutine(loadVideo(camino));
        }
        catch (System.Exception ex)
        {
            LOG = ex.Message;
        }
    }

    IEnumerator loadVideo(string path)
    {
        PanelVideo.gameObject.GetComponentInChildren<VideoPlayer>().url = path;
        PanelVideo.gameObject.GetComponentInChildren<VideoPlayer>().Prepare();

        while (!PanelVideo.gameObject.GetComponentInChildren<VideoPlayer>().isPrepared)
            yield return null;

        PanelVideo.gameObject.GetComponentInChildren<VideoPlayer>().Play();
    }

    private void MostrarTexto()
    {
        EnEditorTexto = true;

        string elTexto = "";
        PanelEditorTexto2.gameObject.SetActive(true);
        StreamReader sr = new StreamReader(camino, System.Text.Encoding.GetEncoding("iso-8859-1"));

        try
        {
            while (!sr.EndOfStream)
            {
                elTexto += sr.ReadLine() + "\n";
            }
            sr.Close();

            if (elTexto.Length > 1)
            {
                PanelEditorTexto2.GetComponentInChildren<InputField>().text = elTexto.Remove(elTexto.Length - 1);
            }
            else
            {
                PanelEditorTexto2.GetComponentInChildren<InputField>().text = "";
            }
        }
        catch
        {
            PanelEditorTexto2.GetComponentInChildren<InputField>().text = "";
        }
    }
    public void SalvarCambiosTexto()
    {
        try
        {
            StreamWriter sw = new StreamWriter(camino, false, System.Text.Encoding.GetEncoding("iso-8859-1"));

            sw.Write(PanelEditorTexto2.GetComponentInChildren<InputField>().text.Replace("\n", System.Environment.NewLine));
            sw.Close();

            SistemaSonidos.instancia.PlayFinalizoCopia();

            //actualizar fecha modificacion y tamaño
            //camino = txtCamino.text;
            //LimpiarTodo();
            //CrearDirectorio();

        }
        catch (System.Exception ex)
        {
            LOG = "Error " + ex.Message;
            SistemaSonidos.instancia.PlayError();
        }
    }
    public void CerrarTextoSinSalvar()
    {
        Paso = false;
        LOG = "";
        EnEditorTexto = false;
        PanelEditorTexto2.gameObject.SetActive(false);
        StartCoroutine(SeguirPasando());
    }

    private void MostrarSFO()
    {
        EnEditorTexto = true;

        string elTexto = "";
        PanelEditorTexto1.gameObject.SetActive(true);

        try
        {
            var sfo = new Param_SFO.PARAM_SFO(camino);

            for (int i = 0; i < sfo.Tables.Count; i++)
            {
                elTexto += sfo.Tables[i].Name + ": " + sfo.Tables[i].Value + "\n";
            }
        }
        catch (System.Exception ex)
        {
            LOG = "Error " + ex.Message;
            SistemaSonidos.instancia.PlayError();
        }

        PanelEditorTexto1.GetComponentInChildren<Text>().text = elTexto;

        Canvas.ForceUpdateCanvases();

        if (PanelEditorTexto1.GetComponentInChildren<Scrollbar>().size == 1)
        {
            PanelEditorTexto1.GetComponentInChildren<Scrollbar>().value = 0;
        }
        else
        {
            PanelEditorTexto1.GetComponentInChildren<Scrollbar>().value = 1;
        }
    }

    private void MostrarPKG()
    {
        EnImagen = true;
        PanelPKG.gameObject.SetActive(true);
    }

    // Opciones /////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void NuevaCarpeta()
    {
        EnOpciones = false;
        PanelOpciones.SetActive(false);

        EnTeclado = true;

        FileFolderAction = ActionsWhichRequiresKeyboard.CreateFolder;

        PanelTeclado.GetComponentInChildren<InputField>().text = "New folder";
        KeyBoardManagerScr.KeyboardOnScreen();
    }

    public void NuevaCarpetaAccion()
    {
        EnTeclado = false;

        string NuevaCarpetaNombre = KeyBoardManagerScr.TargetKeyboardText.text;
        if (NuevaCarpetaNombre == "")
            return;

        try
        {
            if (!Directory.Exists(txtCamino.text + _s_ + NuevaCarpetaNombre))
            {
                Directory.CreateDirectory(txtCamino.text + _s_ + NuevaCarpetaNombre);
            }
            else
            {
                LOG = CadenaErrorCrearCarpeta;
                SistemaSonidos.instancia.PlayError();
                return;
            }

            camino = txtCamino.text;
            LimpiarTodo();
            CrearDirectorio();
        }
        catch (System.Exception ex)
        {
            LOG = "Error " + ex.Message;
            SistemaSonidos.instancia.PlayError();
        }
    }

    public void NuevoFichero()
    {
        EnOpciones = false;
        PanelOpciones.SetActive(false);

        EnTeclado = true;

        FileFolderAction = ActionsWhichRequiresKeyboard.CrearFichero;

        PanelTeclado.GetComponentInChildren<InputField>().text = "New file.txt";
        KeyBoardManagerScr.KeyboardOnScreen();
    }

    public void NuevoFicheroAccion()
    {
        EnTeclado = false;

        string NuevaFicheroNombre = KeyBoardManagerScr.TargetKeyboardText.text;
        if (NuevaFicheroNombre == "")
            return;

        try
        {
            if (!File.Exists(txtCamino.text + _s_ + NuevaFicheroNombre))
            {
                File.CreateText(txtCamino.text + _s_ + NuevaFicheroNombre);
            }
            else
            {
                LOG = CadenaErrorCrearFichero;
                SistemaSonidos.instancia.PlayError();
                return;
            }
        }
        catch (System.Exception ex)
        {
            LOG = "Error " + ex.Message;
            SistemaSonidos.instancia.PlayError();
        }

        camino = txtCamino.text;
        LimpiarTodo();
        CrearDirectorio();
    }

    public void DescargarFichero()
    {
        EnOpciones = false;
        PanelOpciones.SetActive(false);

        EnTeclado = true;

        FileFolderAction = ActionsWhichRequiresKeyboard.DescargarFichero;

        PanelTeclado.GetComponentInChildren<InputField>().text = "http://";
        KeyBoardManagerScr.KeyboardOnScreen();
    }

    public void DescargarFicheroAction()
    {
        EnTeclado = false;

        string FicheroURL = KeyBoardManagerScr.TargetKeyboardText.text;
        if (FicheroURL == "")
            return;

        StartCoroutine(DownloadFichero(FicheroURL));
    }

    IEnumerator DownloadFichero(string FicheroURL)
    {
        using (var www = UnityWebRequest.Get(FicheroURL.Trim()))
        {
            string Destino = txtCamino.text;
            www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                LOG = "Error: " + www.error;
                SistemaSonidos.instancia.PlayError();
            }
            else
            {
                while (!www.isDone)
                {
                    LOG = "Downloading " + (www.downloadProgress * 100).ToString("n2") + " %";
                    yield return null;
                }

                try
                {
                    File.WriteAllBytes(Destino + _s_ + Path.GetFileName(www.url), www.downloadHandler.data);
                    SistemaSonidos.instancia.PlayFinalizoCopia();
                    LOG = "Download completed in: " + Destino;
                }
                catch (Exception ex)
                {
                    LOG = "Error: " + ex.Message;
                    SistemaSonidos.instancia.PlayError();
                }

                yield return null;

                if (Destino == txtCamino.text)
                {
                    camino = txtCamino.text;
                    LimpiarTodo();
                    CrearDirectorio();
                }
            }
        }
    }

    string FicheroACortar = "";
    string FicheroACopiar = "";

    public void Cortar()
    {
        if (camino != "")
        {
            EnOpciones = false;
            PanelOpciones.SetActive(false);

            Color elColor = carpetasPrefab.GetComponentInChildren<Text>().color;
            elColor.a = 0.4f;

            if (!Multiseleccion)
            {
                FicheroACopiar = "";
                FicheroACortar = camino;

                OscurecerCortado();
                ObjetosCreados[Posicion].GetComponent<UnityEngine.UI.Image>().color = new Color(255, 255, 255, 0.4f);
                ObjetosCreados[Posicion].GetComponentInChildren<Text>().color = elColor;
            }
            else
            {
                FicheroACortar = "";

                for (int i = 0; i < ObjetosSelecionados.Count; i++)
                {
                    ObjetosCreados[ObjetosSelecionados[i]].GetComponent<UnityEngine.UI.Image>().color = new Color(255, 255, 255, 0.4f);
                    ObjetosCreados[ObjetosSelecionados[i]].GetComponentInChildren<Text>().color = elColor;
                }
            }

            MultiseleccionCortada = Multiseleccion;
            MultiseleccionCopiada = false;
        }
    }

    private void OscurecerCortado()
    {
        Color elColor = carpetasPrefab.GetComponentInChildren<Text>().color;

        for (int i = 0; i < scaneo.Length; i++)
        {
            ObjetosCreados[i].GetComponent<UnityEngine.UI.Image>().color = new Color(255, 255, 255, 1f);
            ObjetosCreados[i].GetComponentInChildren<Text>().color = elColor;
        }
    }

    public void Copiar()
    {
        if (camino != "")
        {
            EnOpciones = false;
            PanelOpciones.SetActive(false);

            if (!Multiseleccion)
            {
                FicheroACortar = "";
                FicheroACopiar = camino;
            }
            else
            {
                FicheroACopiar = "";
            }

            MultiseleccionCopiada = Multiseleccion;
            MultiseleccionCortada = false;

            OscurecerCortado();
        }
    }

    public void Pegar()
    {
        EnOpciones = false;
        PanelOpciones.SetActive(false);

        if (MultiseleccionCopiada)
        {
            Pegando = true;
            CantArchivos = ObjetosCaminos.Count;

            PanelCopiando.GetComponentInChildren<Slider>().maxValue = CantArchivos;
            PanelCopiando.gameObject.SetActive(true);
            PanelCopiando.GetComponentInChildren<Text>().text = CadenaCopiando;

            try
            {
                StartCoroutine(CopiarMultiplesFichero());
            }
            catch (System.Exception ex)
            {
                LOG = "Error " + ex.Message;
                CantArchivos = 0;
                Pegando = false;
                SistemaSonidos.instancia.PlayError();
            }
        }
        else if (MultiseleccionCortada)
        {
            Pegando = true;
            CantArchivos = ObjetosCaminos.Count;

            PanelCopiando.GetComponentInChildren<Slider>().maxValue = CantArchivos;
            PanelCopiando.gameObject.SetActive(true);
            PanelCopiando.GetComponentInChildren<Text>().text = CadenaMoviendo;

            try
            {
                StartCoroutine(CortarMultiplesFichero());
            }
            catch (System.Exception ex)
            {
                LOG = "Error " + ex.Message;
                CantArchivos = 0;
                Pegando = false;
                SistemaSonidos.instancia.PlayError();
            }
        }
        else
        {
            if (FicheroACortar != "")
            {
                try
                {
                    if (Directory.Exists(FicheroACortar))
                    {
                        if (Path.GetFullPath(FicheroACortar) != Path.GetFullPath(txtCamino.text + _s_ + Path.GetFileName(FicheroACortar)))
                        {
                            Pegando = true;
                            ContarFicheros(FicheroACortar);
                            PanelCopiando.GetComponentInChildren<Slider>().maxValue = CantArchivos;
                            PanelCopiando.gameObject.SetActive(true);
                            PanelCopiando.GetComponentInChildren<Text>().text = CadenaMoviendo;

                            StartCoroutine(MoverDirectorio(FicheroACortar, txtCamino.text + _s_ + Path.GetFileName(FicheroACortar), false));
                            StartCoroutine(EliminarDirectorioMovidos(FicheroACortar));

                            FicheroACortar = "";

                            camino = txtCamino.text;
                            LimpiarTodo();
                            CrearDirectorio();
                        }
                        else
                        {
                            OscurecerCortado();
                        }
                    }
                    else
                    {
                        Pegando = true;
                        CantArchivos = 1;
                        PanelCopiando.GetComponentInChildren<Slider>().maxValue = 1;
                        PanelCopiando.gameObject.SetActive(true);
                        PanelCopiando.GetComponentInChildren<Text>().text = CadenaMoviendo;

                        StartCoroutine(MoverFichero(FicheroACortar, txtCamino.text + _s_ + Path.GetFileName(FicheroACortar)));
                    }
                }
                catch (System.Exception ex)
                {
                    LOG = "Error " + ex.Message;
                    CantArchivos = 0;
                    Pegando = false;
                    SistemaSonidos.instancia.PlayError();
                }
            }
            else if (FicheroACopiar != "")
            {
                try
                {
                    if (Directory.Exists(FicheroACopiar))
                    {
                        Pegando = true;
                        ContarFicheros(FicheroACopiar);
                        PanelCopiando.GetComponentInChildren<Slider>().maxValue = CantArchivos;
                        PanelCopiando.gameObject.SetActive(true);
                        PanelCopiando.GetComponentInChildren<Text>().text = CadenaCopiando;

                        if (Path.GetFullPath(FicheroACopiar) != Path.GetFullPath(txtCamino.text + _s_ + Path.GetFileName(FicheroACopiar)))
                        {
                            StartCoroutine(CopiarDirectorio(FicheroACopiar, txtCamino.text + _s_ + Path.GetFileName(FicheroACopiar), false, true));
                        }
                        else
                        {
                            StartCoroutine(CopiarDirectorio(FicheroACopiar, txtCamino.text + _s_ + Path.GetFileName(FicheroACopiar) + "_copy", false, true));
                        }

                        camino = txtCamino.text;
                        LimpiarTodo();
                        CrearDirectorio();
                    }
                    else
                    {
                        Pegando = true;
                        CantArchivos = 1;
                        PanelCopiando.GetComponentInChildren<Slider>().maxValue = 1;
                        PanelCopiando.gameObject.SetActive(true);
                        PanelCopiando.GetComponentInChildren<Text>().text = CadenaCopiando;

                        StartCoroutine(CopiarFichero(FicheroACopiar, txtCamino.text + _s_ + Path.GetFileName(FicheroACopiar)));
                    }
                }
                catch (System.Exception ex)
                {
                    LOG = "Error " + ex.Message;
                    CantArchivos = 0;
                    Pegando = false;
                    SistemaSonidos.instancia.PlayError();
                }
            }
            else
            {
                LOG = CadenaNoHasCopiado;
            }
        }
    }

    string TextoOriginal = "";
    public void Renombra()
    {
        if (!Multiseleccion && camino != "")
        {
            if (FicheroACopiar == camino)
                FicheroACopiar = "";

            if (FicheroACortar == camino)
                FicheroACortar = "";

            EnOpciones = false;
            PanelOpciones.SetActive(false);

            EnTeclado = true;

            FileFolderAction = ActionsWhichRequiresKeyboard.RenameFile;

            TextoOriginal = Path.GetFileName(camino);
            PanelTeclado.GetComponentInChildren<InputField>().text = TextoOriginal;

            KeyBoardManagerScr.KeyboardOnScreen();
        }
    }

    public void RenombrarAccion()
    {
        EnTeclado = false;

        string TextoCambiado = KeyBoardManagerScr.TargetKeyboardText.text;
        if (TextoCambiado == "")
            return;

        try
        {
            if (TextoOriginal != TextoCambiado)
            {
                if (Directory.Exists(camino))
                {
                    Directory.Move(camino, txtCamino.text + _s_ + TextoCambiado);
                }
                else
                {
                    File.Move(camino, txtCamino.text + _s_ + TextoCambiado);
                }

                camino = txtCamino.text;
                LimpiarTodo();
                CrearDirectorio();
            }
        }
        catch (System.Exception ex)
        {
            LOG = "Error " + ex.Message;
            SistemaSonidos.instancia.PlayError();
        }
    }

    public bool Seguro = false;
    public void Eliminar()
    {
        if (camino != "")
        {
            if (FicheroACopiar == camino)
                FicheroACopiar = "";

            if (FicheroACortar == camino)
                FicheroACortar = "";

            EnOpciones = false;
            PanelOpciones.SetActive(false);

            if (Multiseleccion)
            {
                for (int i = 0; i < ObjetosCaminos.Count; i++)
                {
                    try
                    {
                        if (Directory.Exists(ObjetosCaminos[i]))
                        {
                            Directory.Delete(ObjetosCaminos[i], true);
                        }
                        else
                        {
                            File.Delete(ObjetosCaminos[i]);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        LOG = "Error " + ex.Message;
                        SistemaSonidos.instancia.PlayError();
                    }

                    camino = txtCamino.text;
                    LimpiarTodo();
                    CrearDirectorio();
                }
            }
            else
            {
                try
                {
                    if (Directory.Exists(camino))
                    {
                        Directory.Delete(camino, true);
                    }
                    else
                    {
                        File.Delete(camino);
                    }

                    camino = txtCamino.text;
                    LimpiarTodo();
                    CrearDirectorio();
                }
                catch (System.Exception ex)
                {
                    LOG = "Error " + ex.Message;
                    SistemaSonidos.instancia.PlayError();
                }
            }
        }
    }

    // Auxiliares ///////////////////////////////////////////////////////////////////////////////////////////////////////////

    int CantArchivos = 0;
    void ContarFicheros(string origen)
    {
        CantArchivos = 0;

        string[] files;
        files = Directory.GetFileSystemEntries(origen);
        foreach (string element in files)
        {
            if (Directory.Exists(element))
                ContarFicheros(element);
            else
                CantArchivos++;
        }
    }

    IEnumerator MoverDirectorio(string origen, string destino, bool SiATodo)
    {
        miSlider2.gameObject.SetActive(false);
        BotonCancelar.SetActive(false);

        string[] files;
        if (destino[destino.Length - 1] != Path.DirectorySeparatorChar)
            destino += Path.DirectorySeparatorChar;

        if (!Directory.Exists(destino))
        {
            try
            {
                Directory.CreateDirectory(destino);
            }
            catch (System.Exception ex)
            {
                LOG = "Error " + ex.Message;
                PanelCopiando.gameObject.SetActive(false);
                Pegando = false;
                SistemaSonidos.instancia.PlayError();
                CantArchivos = 0;
                StopAllCoroutines();
            }
        }

        yield return null;

        files = Directory.GetFileSystemEntries(origen);
        foreach (string element in files)
        {
            if (Directory.Exists(element))
                StartCoroutine(MoverDirectorio(element, destino + Path.GetFileName(element), SiATodo));
            else
            {
                if (File.Exists(destino + Path.GetFileName(element)))
                {
                    if (!SiATodo)
                    {
                        PanelMensaje.SetActive(true);
                        PanelMensaje.transform.GetChild(6).gameObject.SetActive(true);
                        EventSystem.current.SetSelectedGameObject(null);
                        EventSystem.current.SetSelectedGameObject(PanelMensaje.transform.GetChild(3).gameObject);
                        PanelMensaje.transform.GetChild(2).gameObject.GetComponent<Text>().text = Path.GetFileName(destino + Path.GetFileName(element));

                        while (AccionSobrescribir == Sobreescribir.Stop)
                            yield return null;
                    }

                    try
                    {
                        switch (AccionSobrescribir)
                        {
                            case Sobreescribir.Si:
                                File.Delete(destino + Path.GetFileName(element));
                                File.Move(element, destino + Path.GetFileName(element));
                                break;
                            case Sobreescribir.SiTodo:
                                SiATodo = true;
                                File.Delete(destino + Path.GetFileName(element));
                                File.Move(element, destino + Path.GetFileName(element));
                                break;
                            case Sobreescribir.No:
                                break;
                            case Sobreescribir.Cancel:
                                PanelCopiando.gameObject.SetActive(false);
                                Pegando = false;
                                CantArchivos = 0;

                                AccionSobrescribir = Sobreescribir.Stop;
                                StopAllCoroutines();
                                break;
                        }

                        CantArchivos--;
                    }
                    catch (System.Exception ex)
                    {
                        LOG = "Error " + ex.Message;
                        PanelCopiando.gameObject.SetActive(false);
                        Pegando = false;
                        SistemaSonidos.instancia.PlayError();
                        CantArchivos = 0;
                        StopAllCoroutines();
                    }

                    AccionSobrescribir = Sobreescribir.Stop;
                }
                else
                {
                    try
                    {
                        File.Move(element, destino + Path.GetFileName(element));
                        CantArchivos--;
                    }
                    catch (System.Exception ex)
                    {
                        LOG = "Error " + ex.Message;
                        PanelCopiando.gameObject.SetActive(false);
                        Pegando = false;
                        SistemaSonidos.instancia.PlayError();
                        CantArchivos = 0;
                        StopAllCoroutines();
                    }
                }
            }

            AccionSobrescribir = Sobreescribir.Stop;
            yield return null;
        }
    }

    IEnumerator EliminarDirectorioMovidos(string origen)
    {
        while (CantArchivos > 0)
            yield return null;

        Directory.Delete(origen, true);
        StopAllCoroutines();
    }

    IEnumerator CopiarDirectorio(string origen, string destino, bool SiATodo, bool Detener)
    {
        miSlider2.gameObject.SetActive(false);
        BotonCancelar.SetActive(false);

        string[] files;
        destino += Path.DirectorySeparatorChar;

        if (!Directory.Exists(destino))
        {
            try
            {
                Directory.CreateDirectory(destino);
            }
            catch (System.Exception ex)
            {
                LOG = "Error " + ex.Message;
                PanelCopiando.gameObject.SetActive(false);
                Pegando = false;
                SistemaSonidos.instancia.PlayError();
                CantArchivos = 0;
                StopAllCoroutines();
            }
        }

        yield return null;

        files = Directory.GetFileSystemEntries(origen);
        foreach (string element in files)
        {
            if (Directory.Exists(element))
                StartCoroutine(CopiarDirectorio(element, destino + Path.GetFileName(element), SiATodo, Detener));
            else
            {
                if (File.Exists(destino + Path.GetFileName(element)))
                {
                    if (!SiATodo)
                    {
                        PanelMensaje.SetActive(true);
                        PanelMensaje.transform.GetChild(6).gameObject.SetActive(true);
                        EventSystem.current.SetSelectedGameObject(null);
                        EventSystem.current.SetSelectedGameObject(PanelMensaje.transform.GetChild(3).gameObject);
                        PanelMensaje.transform.GetChild(2).gameObject.GetComponent<Text>().text = Path.GetFileName(destino + Path.GetFileName(element));

                        while (AccionSobrescribir == Sobreescribir.Stop)
                            yield return null;
                    }

                    try
                    {
                        switch (AccionSobrescribir)
                        {
                            case Sobreescribir.Si:
                                File.Copy(element, destino + Path.GetFileName(element), true);
                                break;
                            case Sobreescribir.SiTodo:
                                SiATodo = true;
                                File.Copy(element, destino + Path.GetFileName(element), true);
                                break;
                            case Sobreescribir.No:
                                break;
                            case Sobreescribir.Cancel:
                                PanelCopiando.gameObject.SetActive(false);
                                Pegando = false;
                                CantArchivos = 0;

                                AccionSobrescribir = Sobreescribir.Stop;
                                StopAllCoroutines();
                                break;
                        }

                        CantArchivos--;
                    }
                    catch (System.Exception ex)
                    {
                        LOG = "Error " + ex.Message;
                        PanelCopiando.gameObject.SetActive(false);
                        Pegando = false;
                        SistemaSonidos.instancia.PlayError();
                        CantArchivos = 0;
                        StopAllCoroutines();
                    }

                    AccionSobrescribir = Sobreescribir.Stop;
                }
                else
                {
                    try
                    {
                        File.Copy(element, destino + Path.GetFileName(element));
                        CantArchivos--;
                    }
                    catch (System.Exception ex)
                    {
                        LOG = "Error " + ex.Message;
                        PanelCopiando.gameObject.SetActive(false);
                        Pegando = false;
                        SistemaSonidos.instancia.PlayError();
                        CantArchivos = 0;
                        StopAllCoroutines();
                    }
                }

                if (CantArchivos == 0 && Detener)
                    StopAllCoroutines();
            }

            AccionSobrescribir = Sobreescribir.Stop;
            yield return null;
        }
    }

    IEnumerator MoverFichero(string origen, string destino)
    {
        miSlider2.gameObject.SetActive(false);
        BotonCancelar.SetActive(false);
        yield return null;

        if (Path.GetFullPath(origen) != Path.GetFullPath(destino))
        {
            if (File.Exists(destino))
            {
                PanelMensaje.SetActive(true);
                PanelMensaje.transform.GetChild(6).gameObject.SetActive(false);
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(PanelMensaje.transform.GetChild(3).gameObject);
                PanelMensaje.transform.GetChild(2).gameObject.GetComponent<Text>().text = Path.GetFileName(destino);

                while (AccionSobrescribir == Sobreescribir.Stop)
                    yield return null;

                try
                {
                    if (AccionSobrescribir == Sobreescribir.Si)
                    {
                        File.Delete(destino);
                        File.Move(origen, destino);
                        FicheroACortar = "";
                    }
                }
                catch (System.Exception ex)
                {
                    LOG = "Error " + ex.Message;
                    PanelCopiando.gameObject.SetActive(false);
                    Pegando = false;
                    SistemaSonidos.instancia.PlayError();
                }
            }
            else
            {
                try
                {
                    File.Move(origen, destino);
                    FicheroACortar = "";
                }
                catch (System.Exception ex)
                {
                    LOG = "Error " + ex.Message;
                    PanelCopiando.gameObject.SetActive(false);
                    Pegando = false;
                    SistemaSonidos.instancia.PlayError();
                }
            }
        }

        CantArchivos = 0;

        camino = txtCamino.text;
        LimpiarTodo();
        CrearDirectorio();

        yield return null;
    }

    IEnumerator CopiarFichero(string origen, string destino)
    {
        miSlider2.gameObject.SetActive(true);
        BotonCancelar.SetActive(true);
        yield return null;

        if (Path.GetFullPath(origen) == Path.GetFullPath(destino))
        {
            destino = destino.Replace(Path.GetFileName(destino), Path.GetFileNameWithoutExtension(destino) + "_copy" + Path.GetExtension(destino));
        }

        bool Ok = false;
        if (File.Exists(destino))
        {
            PanelMensaje.SetActive(true);
            PanelMensaje.transform.GetChild(6).gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(PanelMensaje.transform.GetChild(3).gameObject);
            PanelMensaje.transform.GetChild(2).gameObject.GetComponent<Text>().text = Path.GetFileName(destino);

            while (AccionSobrescribir == Sobreescribir.Stop)
                yield return null;

            if (AccionSobrescribir == Sobreescribir.Si)
            {
                try
                {
                    File.Delete(destino);

                    byte[] buffer = new byte[1024 * 1024]; // 1MB buffer
                    using (FileStream source = new FileStream(origen, FileMode.Open, FileAccess.Read))
                    {
                        FileInfo fileData = new FileInfo(origen);
                        float fileLength = fileData.Length / 1024f;

                        using (FileStream dest = new FileStream(destino, FileMode.CreateNew, FileAccess.Write))
                        {
                            float totalBytes = 0;
                            float persentage = 0;
                            int currentBlockSize = 0;

                            while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                totalBytes += currentBlockSize / 1024f;
                                persentage = totalBytes / fileLength;

                                dest.Write(buffer, 0, currentBlockSize);

                                miSlider2.value = persentage;
                                TextPorciento.text = (persentage * 100f).ToString("n2") + " %";
                                yield return null;

                                if (CancelarPegado)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    Ok = true;
                }
                finally
                {
                    if (!Ok)
                    {
                        LOG = "Error !";
                        CantArchivos = 0;
                        PanelCopiando.gameObject.SetActive(false);
                        Pegando = false;
                        SistemaSonidos.instancia.PlayError();
                    }
                    else
                    {
                        if (CancelarPegado)
                        {
                            LOG = "Cancelled !";
                            CantArchivos = 0;
                            PanelCopiando.gameObject.SetActive(false);
                            Pegando = false;
                            SistemaSonidos.instancia.PlayError();
                        }
                    }
                }
            }
        }
        else
        {
            try
            {
                byte[] buffer = new byte[1024 * 1024]; // 1MB buffer

                using (FileStream source = new FileStream(origen, FileMode.Open, FileAccess.Read))
                {
                    FileInfo fileData = new FileInfo(origen);
                    float fileLength = fileData.Length / 1024f;

                    using (FileStream dest = new FileStream(destino, FileMode.CreateNew, FileAccess.Write))
                    {
                        float totalBytes = 0;
                        float persentage = 0;
                        int currentBlockSize = 0;

                        while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            totalBytes += currentBlockSize / 1024f;
                            persentage = totalBytes / fileLength;

                            dest.Write(buffer, 0, currentBlockSize);

                            miSlider2.value = persentage;
                            TextPorciento.text = (persentage * 100f).ToString("n2") + " %";
                            yield return null;

                            if (CancelarPegado)
                            {
                                break;
                            }
                        }
                    }
                }

                Ok = true;
            }
            finally
            {
                if (!Ok)
                {
                    LOG = "Error !";
                    CantArchivos = 0;
                    PanelCopiando.gameObject.SetActive(false);
                    Pegando = false;
                    SistemaSonidos.instancia.PlayError();
                }
                else
                {
                    if (CancelarPegado)
                    {
                        LOG = "Cancelled !";
                        CantArchivos = 0;
                        PanelCopiando.gameObject.SetActive(false);
                        Pegando = false;
                        SistemaSonidos.instancia.PlayError();
                    }
                }
            }
        }

        CantArchivos = 0;
        yield return null;

        if (CancelarPegado)
        {
            CancelarPegado = false;
            File.Delete(destino);
        }

        camino = txtCamino.text;
        LimpiarTodo();
        CrearDirectorio();

        yield return null;
    }

    IEnumerator CopiarMultiplesFichero()
    {
        miSlider2.gameObject.SetActive(false);
        BotonCancelar.SetActive(false);

        bool SiATodo = false;
        yield return null;

        for (int i = 0; i < ObjetosCaminos.Count; i++)
        {
            if (File.Exists(txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i])))
            {
                if (!SiATodo)
                {
                    PanelMensaje.SetActive(true);
                    PanelMensaje.transform.GetChild(6).gameObject.SetActive(true);
                    EventSystem.current.SetSelectedGameObject(null);
                    EventSystem.current.SetSelectedGameObject(PanelMensaje.transform.GetChild(3).gameObject);
                    PanelMensaje.transform.GetChild(2).gameObject.GetComponent<Text>().text = Path.GetFileName(txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i]));

                    while (AccionSobrescribir == Sobreescribir.Stop)
                        yield return null;

                    yield return null;
                }

                try
                {
                    switch (AccionSobrescribir)
                    {
                        case Sobreescribir.Si:
                            File.Copy(ObjetosCaminos[i], txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i]), true);
                            break;
                        case Sobreescribir.SiTodo:
                            SiATodo = true;
                            File.Copy(ObjetosCaminos[i], txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i]), true);
                            break;
                        case Sobreescribir.Cancel:
                            PanelCopiando.gameObject.SetActive(false);
                            Pegando = false;
                            CantArchivos = 0;

                            AccionSobrescribir = Sobreescribir.Stop;

                            camino = txtCamino.text;
                            LimpiarTodo();
                            CrearDirectorio();

                            StopAllCoroutines();
                            break;
                    }

                    CantArchivos--;
                }
                catch (System.Exception ex)
                {
                    LOG = "Error " + ex.Message;
                    PanelCopiando.gameObject.SetActive(false);
                    Pegando = false;
                    CantArchivos = 0;
                    AccionSobrescribir = Sobreescribir.Stop;
                    SistemaSonidos.instancia.PlayError();
                    StopAllCoroutines();
                }

                if (!SiATodo)
                {
                    AccionSobrescribir = Sobreescribir.Stop;
                }
            }
            else
            {
                if (Directory.Exists(ObjetosCaminos[i]))
                {
                    // TEST
                    int TempCA = CantArchivos;

                    ContarFicheros(ObjetosCaminos[i]);
                    StartCoroutine(CopiarDirectorio(ObjetosCaminos[i], txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i]), SiATodo, false));

                    while (CantArchivos > 0)
                        yield return null;

                    CantArchivos = TempCA - 1;
                    // TEST
                }
                else
                {
                    try
                    {
                        File.Copy(ObjetosCaminos[i], txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i]));
                        CantArchivos--;
                    }
                    catch (System.Exception ex)
                    {
                        LOG = "Error " + ex.Message;
                        PanelCopiando.gameObject.SetActive(false);
                        Pegando = false;
                        CantArchivos = 0;
                        SistemaSonidos.instancia.PlayError();
                        StopAllCoroutines();
                    }
                }
            }

            if (!SiATodo)
            {
                AccionSobrescribir = Sobreescribir.Stop;
            }
            yield return null;
        }

        if (CantArchivos == 0)
        {
            camino = txtCamino.text;
            LimpiarTodo();
            CrearDirectorio();
        }
    }

    IEnumerator CortarMultiplesFichero()
    {
        miSlider2.gameObject.SetActive(false);
        BotonCancelar.SetActive(false);

        bool SiATodo = false;
        yield return null;

        for (int i = 0; i < ObjetosCaminos.Count; i++)
        {
            if (Path.GetFullPath(ObjetosCaminos[i]) != Path.GetFullPath(txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i])))
            {
                if (File.Exists(txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i])))
                {
                    if (!SiATodo)
                    {
                        PanelMensaje.SetActive(true);
                        PanelMensaje.transform.GetChild(6).gameObject.SetActive(true);
                        EventSystem.current.SetSelectedGameObject(null);
                        EventSystem.current.SetSelectedGameObject(PanelMensaje.transform.GetChild(3).gameObject);
                        PanelMensaje.transform.GetChild(2).gameObject.GetComponent<Text>().text = Path.GetFileName(txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i]));

                        while (AccionSobrescribir == Sobreescribir.Stop)
                            yield return null;

                        yield return null;
                    }

                    try
                    {
                        switch (AccionSobrescribir)
                        {
                            case Sobreescribir.Si:
                                File.Delete(txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i]));
                                File.Move(ObjetosCaminos[i], txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i]));
                                break;
                            case Sobreescribir.SiTodo:
                                SiATodo = true;
                                File.Delete(txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i]));
                                File.Move(ObjetosCaminos[i], txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i]));
                                break;
                            case Sobreescribir.Cancel:
                                PanelCopiando.gameObject.SetActive(false);
                                Pegando = false;
                                CantArchivos = 0;

                                AccionSobrescribir = Sobreescribir.Stop;

                                camino = txtCamino.text;
                                LimpiarTodo();
                                CrearDirectorio();

                                StopAllCoroutines();
                                break;
                        }

                        CantArchivos--;
                    }
                    catch (System.Exception ex)
                    {
                        LOG = "Error " + ex.Message;
                        PanelCopiando.gameObject.SetActive(false);
                        Pegando = false;
                        CantArchivos = 0;
                        AccionSobrescribir = Sobreescribir.Stop;
                        SistemaSonidos.instancia.PlayError();
                        StopAllCoroutines();
                    }

                    if (!SiATodo)
                    {
                        AccionSobrescribir = Sobreescribir.Stop;
                    }
                }
                else
                {
                    if (Directory.Exists(ObjetosCaminos[i]))
                    {
                        // TEST
                        int TempCA = CantArchivos;

                        ContarFicheros(ObjetosCaminos[i]);
                        StartCoroutine(MoverDirectorio(ObjetosCaminos[i], txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i]), SiATodo));

                        while (CantArchivos > 0)
                            yield return null;

                        CantArchivos = TempCA - 1;

                        try
                        {
                            Directory.Delete(ObjetosCaminos[i], true);
                        }
                        catch {; }
                        // TEST
                    }
                    else
                    {
                        try
                        {
                            File.Move(ObjetosCaminos[i], txtCamino.text + _s_ + Path.GetFileName(ObjetosCaminos[i]));
                            CantArchivos--;
                        }
                        catch (System.Exception ex)
                        {
                            LOG = "Error " + ex.Message;
                            PanelCopiando.gameObject.SetActive(false);
                            Pegando = false;
                            CantArchivos = 0;
                            SistemaSonidos.instancia.PlayError();
                            StopAllCoroutines();
                        }
                    }
                }
            }
            else
            {
                CantArchivos--;
            }

            if (!SiATodo)
            {
                AccionSobrescribir = Sobreescribir.Stop;
            }
            yield return null;
        }

        if (CantArchivos == 0)
        {
            MultiseleccionCortada = false;
            camino = txtCamino.text;
            LimpiarTodo();
            CrearDirectorio();
        }
    }

    // Opciones avanzadas /////////////////////////////////////////////////////////////////////////////////////////////////////////
    bool FtpActivado = false;
    bool FullRwActivado = false;

    public void ActivarFTP()
    {
        if (FW > 0)
        {
            try
            {
                if (!FtpActivado)
                {
                    FreeFTP();
                    FtpActivado = true;
                }

                LOG = CadenaFtpActivo + " - I.P: " + Network.player.ipAddress + ", Port: 21";
            }
            catch (System.Exception ex)
            {
                LOG = "Error " + ex.Message;
                SistemaSonidos.instancia.PlayError();
            }
        }
    }

    public void ActivarFullRW()
    {
        if (FW > 0)
        {
            try
            {
                if (!FullRwActivado)
                {
                    FreeMount();
                    FullRwActivado = true;
                }

                LOG = CadenaRwActivo;
            }
            catch (System.Exception ex)
            {
                LOG = "Error " + ex.Message;
                SistemaSonidos.instancia.PlayError();
            }
        }
    }

    public void SalvarHome()
    {
        PlayerPrefs.SetInt("Nivel", Nivel);
        PlayerPrefs.SetString("Home", txtCamino.text);
        PlayerPrefs.Save();

        LOG = CadenaHomeSalvado;
    }

    // descompactador
    private void Descompactar(string tipo)
    {
        PanelDescompactador.GetComponentInChildren<Slider>().value = 0;
        EnEditorTexto = true;

        string elTexto = "";
        int Cantidad = 0;
        PanelDescompactador.gameObject.SetActive(true);

        switch (tipo)
        {
            case ".zip":
                using (Stream stream = File.OpenRead(camino))
                using (var reader = SharpCompress.Reader.Zip.ZipReader.Open(stream))
                {
                    while (reader.MoveToNextEntry())
                    {
                        if (!reader.Entry.IsDirectory)
                        {
                            elTexto += reader.Entry.FilePath + "\n";
                            Cantidad++;
                        }
                    }
                }
                break;
            case ".rar":
                using (Stream stream = File.OpenRead(camino))
                using (var reader = SharpCompress.Reader.Rar.RarReader.Open(stream))
                {
                    while (reader.MoveToNextEntry())
                    {
                        if (!reader.Entry.IsDirectory)
                        {
                            elTexto += reader.Entry.FilePath + "\n";
                            Cantidad++;
                        }
                    }
                }
                break;
            case ".tar":
                using (Stream stream = File.OpenRead(camino))
                using (var reader = SharpCompress.Reader.Tar.TarReader.Open(stream))
                {
                    while (reader.MoveToNextEntry())
                    {
                        if (!reader.Entry.IsDirectory)
                        {
                            elTexto += reader.Entry.FilePath + "\n";
                            Cantidad++;
                        }
                    }
                }
                break;
        }

        PanelDescompactador.GetComponentInChildren<Slider>().maxValue = Cantidad;

        // mostrar en texto
        PanelDescompactador.GetComponentInChildren<Text>().text = elTexto;
        Canvas.ForceUpdateCanvases();

        if (PanelDescompactador.GetComponentInChildren<Scrollbar>().size == 1)
        {
            PanelDescompactador.GetComponentInChildren<Scrollbar>().value = 0;
        }
        else
        {
            PanelDescompactador.GetComponentInChildren<Scrollbar>().value = 1;
        }
    }

    IEnumerator DescompactarAccion(string tipo)
    {
        bool Ok = false;
        PanelDescompactador.transform.GetChild(1).GetComponentInChildren<Text>().text = CadenaEspere;
        PanelDescompactador.transform.GetChild(1).GetComponent<Image>().enabled = false;
        yield return null;

        try
        {
            string destino = txtCamino.text + _s_ + Path.GetFileNameWithoutExtension(camino);
            //string destino = @"F:\" + Path.GetFileNameWithoutExtension(camino);
            if (!Directory.Exists(destino))
                Directory.CreateDirectory(destino);

            switch (tipo)
            {
                case ".zip":
                    using (Stream stream = File.OpenRead(camino))
                    using (var reader = SharpCompress.Reader.Zip.ZipReader.Open(stream))
                    {
                        int Valor = 0;
                        while (reader.MoveToNextEntry())
                        {
                            if (reader.Entry.IsDirectory)
                            {
                                Directory.CreateDirectory(destino + _s_ + reader.Entry.FilePath);
                            }
                            else
                            {
                                if (!Directory.Exists(Path.GetDirectoryName(destino + _s_ + reader.Entry.FilePath)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(destino + _s_ + reader.Entry.FilePath));
                                }
                                using (Stream newStream = File.Create(destino + _s_ + reader.Entry.FilePath))
                                {
                                    reader.WriteEntryTo(newStream);
                                    Valor++;
                                    PanelDescompactador.GetComponentInChildren<Slider>().value = Valor;
                                    yield return null;
                                }
                            }
                        }
                    }
                    break;
                case ".rar":
                    using (Stream stream = File.OpenRead(camino))
                    using (var reader = SharpCompress.Reader.Rar.RarReader.Open(stream))
                    {
                        int Valor = 0;
                        while (reader.MoveToNextEntry())
                        {
                            if (reader.Entry.IsDirectory)
                            {
                                Directory.CreateDirectory(destino + _s_ + reader.Entry.FilePath);
                            }
                            else
                            {
                                if (!Directory.Exists(Path.GetDirectoryName(destino + _s_ + reader.Entry.FilePath)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(destino + _s_ + reader.Entry.FilePath));
                                }
                                using (Stream newStream = File.Create(destino + _s_ + reader.Entry.FilePath))
                                {
                                    reader.WriteEntryTo(newStream);
                                    Valor++;
                                    PanelDescompactador.GetComponentInChildren<Slider>().value = Valor;
                                    yield return null;
                                }
                            }
                        }
                    }
                    break;
                case ".tar":
                    using (Stream stream = File.OpenRead(camino))
                    using (var reader = SharpCompress.Reader.Tar.TarReader.Open(stream))
                    {
                        int Valor = 0;
                        while (reader.MoveToNextEntry())
                        {
                            if (reader.Entry.IsDirectory)
                            {
                                Directory.CreateDirectory(destino + _s_ + reader.Entry.FilePath);
                            }
                            else
                            {
                                if (!Directory.Exists(Path.GetDirectoryName(destino + _s_ + reader.Entry.FilePath)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(destino + _s_ + reader.Entry.FilePath));
                                }
                                using (Stream newStream = File.Create(destino + _s_ + reader.Entry.FilePath))
                                {
                                    reader.WriteEntryTo(newStream);
                                    Valor++;
                                    PanelDescompactador.GetComponentInChildren<Slider>().value = Valor;
                                    yield return null;
                                }
                            }
                        }
                    }
                    break;
            }

            Ok = true;
        }
        finally
        {
            if (!Ok)
            {
                LOG = "Error !";
                SistemaSonidos.instancia.PlayError();
            }
            else
            {
                SistemaSonidos.instancia.PlayFinalizoCopia();
            }

            EnEditorTexto = false;
            PanelDescompactador.gameObject.SetActive(false);
            PanelDescompactador.transform.GetChild(1).GetComponentInChildren<Text>().text = CadenaExtraer;
            PanelDescompactador.transform.GetChild(1).GetComponent<Image>().enabled = true;
            Extrayendo = false;

            camino = txtCamino.text;
            LimpiarTodo();
            CrearDirectorio();
        }
    }

    private void DescompactarTema(string fichero_tema)
    {
        string destino = "/data/PS4Xplorer_Theme/";

        if (!Directory.Exists(destino))
            Directory.CreateDirectory(destino);

        // eliminar todo lo q este dentro de la carpeta
        string[] ficheros = Directory.GetFileSystemEntries(destino);
        foreach (string fichero in ficheros)
            File.Delete(fichero);

        // descompactar
        using (Stream stream = File.OpenRead(fichero_tema))
        using (var reader = SharpCompress.Reader.Zip.ZipReader.Open(stream))
        {
            while (reader.MoveToNextEntry())
            {
                if (reader.Entry.IsDirectory)
                {
                    Directory.CreateDirectory(destino + _s_ + reader.Entry.FilePath);
                }
                else
                {
                    if (!Directory.Exists(Path.GetDirectoryName(destino + _s_ + reader.Entry.FilePath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destino + _s_ + reader.Entry.FilePath));
                    }
                    using (Stream newStream = File.Create(destino + _s_ + reader.Entry.FilePath))
                    {
                        reader.WriteEntryTo(newStream);
                    }
                }
            }
        }

        ResetearIconos();
        CargarTema();

        camino = txtCamino.text;
        LimpiarTodo();
        CrearDirectorio();
    }

    public void DescompactarAvatar(string fichero_avatar, string Destino)
    {
        string destino = Destino;

        if (Destino == "")
        {
            string[] MisProfiles = Directory.GetFileSystemEntries("/system_data/priv/cache/profile/");
            if (MisProfiles.Length > 1)
            {
                EnEditorTexto = true;
                PanelSelecionUsuario.SetActive(true);
                return;
            }
            else
            {
                if (Directory.Exists(MisProfiles[0]))
                {
                    destino = MisProfiles[0];
                }
            }
        }
        else
        {
            EnEditorTexto = false;
            PanelSelecionUsuario.SetActive(false);

            string[] MisProfiles = Directory.GetFileSystemEntries("/system_data/priv/cache/profile/");
            foreach (string carpeta in MisProfiles)
            {
                if (carpeta.IndexOf(Destino) > 0)
                {
                    destino = carpeta;
                }
            }

            fichero_avatar = camino;
        }

        if (destino != "")
        {
            // descompactar
            try
            {
                if (Directory.Exists(destino))
                {
                    using (Stream stream = File.OpenRead(fichero_avatar))
                    using (var reader = SharpCompress.Reader.Zip.ZipReader.Open(stream))
                    {
                        while (reader.MoveToNextEntry())
                        {
                            if (File.Exists(destino + _s_ + reader.Entry.FilePath))
                            {
                                File.Delete(destino + _s_ + reader.Entry.FilePath);
                            }
                            using (Stream newStream = File.Create(destino + _s_ + reader.Entry.FilePath))
                            {
                                reader.WriteEntryTo(newStream);
                            }
                        }
                    }

                    LOG = CadenaAvatarInstalado;
                    SistemaSonidos.instancia.PlayFinalizoCopia();
                }
            }
            catch (System.Exception ex)
            {
                LOG = "Error " + ex.Message;
                SistemaSonidos.instancia.PlayError();
            }
        }
    }

    private void ResetearIconos()
    {
        carpetasPrefab.GetComponent<Image>().sprite = carpetasPrefab_Spr;
        carpetasPrefabEspecial.GetComponent<Image>().sprite = carpetasPrefabEspecial_Spr;
        carpetasPrefabBlackList.GetComponent<Image>().sprite = carpetasPrefabBlackList_Spr;
        ficherosPrefab.GetComponent<Image>().sprite = ficherosPrefab_Spr;
        ficherosPrefabFOT.GetComponent<Image>().sprite = ficherosPrefabFOT_Spr;
        ficherosPrefabMP3.GetComponent<Image>().sprite = ficherosPrefabMP3_Spr;
        ficherosPrefabMP4.GetComponent<Image>().sprite = ficherosPrefabMP4_Spr;
        ficherosPrefabTXT.GetComponent<Image>().sprite = ficherosPrefabTXT_Spr;
        ficherosPrefabTHEME.GetComponent<Image>().sprite = ficherosPrefabTHEME_Spr;
        ficherosPrefabRAR.GetComponent<Image>().sprite = ficherosPrefabRAR_Spr;
        ficherosPrefabPKG.GetComponent<Image>().sprite = ficherosPrefabPKG_Spr;
        ficherosPrefabSFO.GetComponent<Image>().sprite = ficherosPrefabSFO_Spr;
        ficherosPrefabAVATAR.GetComponent<Image>().sprite = ficherosPrefabAVATAR_Spr;
        textoOptions.GetComponentInParent<Image>().sprite = IconoOpciones;

        PanelOpciones.transform.GetChild(0).GetComponent<Image>().sprite = carpetasPrefab_Spr;
        PanelOpciones.transform.GetChild(1).GetComponent<Image>().sprite = ficherosPrefabTXT_Spr;

        PanelOpciones.transform.GetChild(3).GetComponent<Image>().sprite = IconoCortar;
        PanelOpciones.transform.GetChild(4).GetComponent<Image>().sprite = IconoCopiar;
        PanelOpciones.transform.GetChild(5).GetComponent<Image>().sprite = IconoPegar;
        PanelOpciones.transform.GetChild(6).GetComponent<Image>().sprite = IconoRenombrar;
        PanelOpciones.transform.GetChild(7).GetComponent<Image>().sprite = IconoEliminar;

        PanelOpcionesAvanzadas.transform.GetChild(1).GetComponent<Image>().sprite = IconoFTP;
        PanelOpcionesAvanzadas.transform.GetChild(2).GetComponent<Image>().sprite = IconoRW;
        PanelOpcionesAvanzadas.transform.GetChild(3).GetComponent<Image>().sprite = IconoHOME;

        PanelCopiando.transform.GetChild(0).GetComponent<Image>().sprite = IconoPegar;
        PanelCopiando.transform.GetChild(4).GetComponent<Image>().sprite = IconoCuadrado;

        PanelMensaje.GetComponent<Image>().sprite = ImagenDefectoOverwrite;

        PanelDescompactador.transform.GetChild(1).GetComponent<Image>().sprite = IconoCuadrado;
    }

    private void CargarTema()
    {
        string CaminoTemas = "/data/PS4Xplorer_Theme/";

        if (File.Exists(CaminoTemas + _s_ + "theme"))
        {
            StreamReader sr = new StreamReader(CaminoTemas + _s_ + "theme");
            Color c = new Color();

            // Main Windows.....................................
            // Color del BG
            string Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color Ventanas_BG = c;

            // Color del INTERIOR
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color Ventanas_IN = c;

            // Color de las Fonts fondo
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color FuenteBG = c;

            // Color de las Fonts interior
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color FuenteIN = c;

            // Color de la barra de selecion
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color Barra_Selector = c;

            // Color de la barra de marca multiselecion
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color Barra_Selector2 = c;

            // Color de las Opciones
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color Opciones_Color = c;

            // Color de las Fonts de las Opciones
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color Opciones_Fuentes = c;

            // Color de la barra de selecion de las Opciones
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color Opciones_Selector = c;

            // Color de las Opciones Avanzadas
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color OpcionesAvanzadas_Color = c;

            // Color de las Fonts de las Opciones Avanzadas
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color OpcionesAvanzadas_Fuentes = c;

            // Color de la barra de selecion de las Opciones Avanzadas
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color OpcionesAvanzadas_Selector = c;

            // Color del Panel Copia y Pega
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color CopiaPegaColor = c;

            // Color de la Font del Panel Copia y Pega
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color CopiaPegaColorFont = c;

            // Color del BACK Progress Bar del Panel Copia y Pega
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color CopiaPegaProgressBarBACK = c;

            // Color del FILL Progress Bar del Panel Copia y Pega
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color CopiaPegaProgressBarFILL = c;

            // Color de la FONT Progress Bar del Panel Copia y Pega
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color CopiaPegaProgressBarFONT = c;

            // Color de la FONT del Overwrite
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorFontOverwrite = c;

            // Color de los Botones del Overwrite
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorBotonesOverwrite = c;

            // Color de los Botones Selecionado del Overwrite
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorBotonSelecionadoOverwrite = c;

            // Color de la FONT de los Botones del Overwrite
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorBotonesFontOverwrite = c;

            // Color Panel textos
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorPanelTextos = c;

            // Color Panel textos Inside
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorPanelTextosInside = c;

            // Color Panel textos Fonts
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorPanelTextosFont = c;

            // Color Panel ZIP and RAR
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorPanelZipRar = c;

            // Color Panel ZIP and RAR Font
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorPanelZipRarFont = c;

            // Color Panel ZIP and RAR Inside
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorPanelZipRarInside = c;

            // Color Panel ZIP and RAR Inside Font
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorPanelZipRarInsideFont = c;

            // Color Panel ZIP and RAR ProgressBar BACK
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorPanelProgresBACK = c;

            // Color Panel ZIP and RAR ProgressBar FILL
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorPanelProgresFILL = c;

            // Color Panel PKG
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorPanelPKG = c;

            // Color Panel PKG Inside
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorPanelPKG_Inside = c;

            // Color Panel PKG Inside Font
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorPanelPKG_Font = c;

            // Scroll Bar Main Windows BACK
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorScrollBarBACK_1 = c;

            // Scroll Bar Main Windows HANDLE
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorScrollBarHANDLE_1 = c;

            // Scroll Bar Textos BACK
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorScrollBarBACK_2 = c;

            // Scroll Bar Textos HANDLE
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorScrollBarHANDLE_2 = c;

            // Scroll Bar Zips BACK
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorScrollBarBACK_3 = c;

            // Scroll Bar Zips HANDLE
            Linea = sr.ReadLine();
            ColorUtility.TryParseHtmlString(Linea, out c);
            Color ColorScrollBarHANDLE_3 = c;

            sr.Close();

            // GB Image
            if (File.Exists(CaminoTemas + _s_ + "Background.png"))
            {
                ImagenFondo.sprite = LeeImagenTema(CaminoTemas + _s_ + "Background.png");
            }
            else
            {
                ImagenFondo.sprite = null;
            }

            // Inside Image
            if (File.Exists(CaminoTemas + _s_ + "Inside.png"))
            {
                scrollRect.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Inside.png");
            }
            else
            {
                scrollRect.GetComponent<Image>().sprite = null;
            }

            // Overwrite Image
            if (File.Exists(CaminoTemas + _s_ + "Overwrite.png"))
            {
                PanelMensaje.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Overwrite.png");
            }

            // COLORES de la Main Windows
            ImagenFondo.color = Ventanas_BG;
            scrollRect.gameObject.GetComponent<Image>().color = Ventanas_IN;

            txtCamino.color = FuenteBG;
            txtTemperatura.color = FuenteBG;
            txtCantidad.color = FuenteBG;
            txtLog.color = FuenteBG;
            textoOptions.color = FuenteBG;

            //carpetasPrefab.GetComponentInChildren<Text>().color = FuenteIN;
            carpetasPrefab.GetComponentsInChildren<Text>()[0].color = FuenteIN;
            carpetasPrefab.GetComponentsInChildren<Text>()[1].color = FuenteIN;
            carpetasPrefab.transform.GetChild(0).GetComponent<Image>().color = Barra_Selector;
            carpetasPrefab.transform.GetChild(1).GetComponent<Image>().color = Barra_Selector2;
            //carpetasPrefabBlackList.GetComponentInChildren<Text>().color = FuenteIN;
            carpetasPrefabBlackList.GetComponentsInChildren<Text>()[0].color = FuenteIN;
            carpetasPrefabBlackList.GetComponentsInChildren<Text>()[1].color = FuenteIN;
            carpetasPrefabBlackList.transform.GetChild(0).GetComponent<Image>().color = Barra_Selector;
            carpetasPrefabBlackList.transform.GetChild(1).GetComponent<Image>().color = Barra_Selector2;
            //carpetasPrefabEspecial.GetComponentInChildren<Text>().color = FuenteIN;
            carpetasPrefabEspecial.GetComponentsInChildren<Text>()[0].color = FuenteIN;
            carpetasPrefabEspecial.GetComponentsInChildren<Text>()[1].color = FuenteIN;
            carpetasPrefabEspecial.transform.GetChild(0).GetComponent<Image>().color = Barra_Selector;
            carpetasPrefabEspecial.transform.GetChild(1).GetComponent<Image>().color = Barra_Selector2;

            ficherosPrefab.GetComponentsInChildren<Text>()[0].color = FuenteIN;
            ficherosPrefab.GetComponentsInChildren<Text>()[1].color = FuenteIN;
            ficherosPrefab.transform.GetChild(0).GetComponent<Image>().color = Barra_Selector;
            ficherosPrefab.transform.GetChild(1).GetComponent<Image>().color = Barra_Selector2;
            ficherosPrefabMP3.GetComponentsInChildren<Text>()[0].color = FuenteIN;
            ficherosPrefabMP3.GetComponentsInChildren<Text>()[1].color = FuenteIN;
            ficherosPrefabMP3.transform.GetChild(0).GetComponent<Image>().color = Barra_Selector;
            ficherosPrefabMP3.transform.GetChild(1).GetComponent<Image>().color = Barra_Selector2;
            ficherosPrefabMP4.GetComponentsInChildren<Text>()[0].color = FuenteIN;
            ficherosPrefabMP4.GetComponentsInChildren<Text>()[1].color = FuenteIN;
            ficherosPrefabMP4.transform.GetChild(0).GetComponent<Image>().color = Barra_Selector;
            ficherosPrefabMP4.transform.GetChild(1).GetComponent<Image>().color = Barra_Selector2;
            ficherosPrefabFOT.GetComponentsInChildren<Text>()[0].color = FuenteIN;
            ficherosPrefabFOT.GetComponentsInChildren<Text>()[1].color = FuenteIN;
            ficherosPrefabFOT.transform.GetChild(0).GetComponent<Image>().color = Barra_Selector;
            ficherosPrefabFOT.transform.GetChild(1).GetComponent<Image>().color = Barra_Selector2;
            ficherosPrefabTXT.GetComponentsInChildren<Text>()[0].color = FuenteIN;
            ficherosPrefabTXT.GetComponentsInChildren<Text>()[1].color = FuenteIN;
            ficherosPrefabTXT.transform.GetChild(0).GetComponent<Image>().color = Barra_Selector;
            ficherosPrefabTXT.transform.GetChild(1).GetComponent<Image>().color = Barra_Selector2;
            ficherosPrefabSFO.GetComponentsInChildren<Text>()[0].color = FuenteIN;
            ficherosPrefabSFO.GetComponentsInChildren<Text>()[1].color = FuenteIN;
            ficherosPrefabSFO.transform.GetChild(0).GetComponent<Image>().color = Barra_Selector;
            ficherosPrefabSFO.transform.GetChild(1).GetComponent<Image>().color = Barra_Selector2;
            ficherosPrefabPKG.GetComponentsInChildren<Text>()[0].color = FuenteIN;
            ficherosPrefabPKG.GetComponentsInChildren<Text>()[1].color = FuenteIN;
            ficherosPrefabPKG.transform.GetChild(0).GetComponent<Image>().color = Barra_Selector;
            ficherosPrefabPKG.transform.GetChild(1).GetComponent<Image>().color = Barra_Selector2;
            ficherosPrefabRAR.GetComponentsInChildren<Text>()[0].color = FuenteIN;
            ficherosPrefabRAR.GetComponentsInChildren<Text>()[1].color = FuenteIN;
            ficherosPrefabRAR.transform.GetChild(0).GetComponent<Image>().color = Barra_Selector;
            ficherosPrefabRAR.transform.GetChild(1).GetComponent<Image>().color = Barra_Selector2;
            ficherosPrefabTHEME.GetComponentsInChildren<Text>()[0].color = FuenteIN;
            ficherosPrefabTHEME.GetComponentsInChildren<Text>()[1].color = FuenteIN;
            ficherosPrefabTHEME.transform.GetChild(0).GetComponent<Image>().color = Barra_Selector;
            ficherosPrefabTHEME.transform.GetChild(1).GetComponent<Image>().color = Barra_Selector2;
            ficherosPrefabAVATAR.GetComponentsInChildren<Text>()[0].color = FuenteIN;
            ficherosPrefabAVATAR.GetComponentsInChildren<Text>()[1].color = FuenteIN;
            ficherosPrefabAVATAR.transform.GetChild(0).GetComponent<Image>().color = Barra_Selector;
            ficherosPrefabAVATAR.transform.GetChild(1).GetComponent<Image>().color = Barra_Selector2;

            // COLORES de la Opciones
            PanelOpciones.GetComponent<Image>().color = Opciones_Color;
            PanelOpciones.transform.GetChild(0).GetComponentInChildren<Text>().color = Opciones_Fuentes;
            PanelOpciones.transform.GetChild(1).GetComponentInChildren<Text>().color = Opciones_Fuentes;
            PanelOpciones.transform.GetChild(2).GetComponentInChildren<Text>().color = Opciones_Fuentes;
            PanelOpciones.transform.GetChild(3).GetComponentInChildren<Text>().color = Opciones_Fuentes;
            PanelOpciones.transform.GetChild(4).GetComponentInChildren<Text>().color = Opciones_Fuentes;
            PanelOpciones.transform.GetChild(5).GetComponentInChildren<Text>().color = Opciones_Fuentes;
            PanelOpciones.transform.GetChild(6).GetComponentInChildren<Text>().color = Opciones_Fuentes;
            PanelOpciones.transform.GetChild(7).GetComponentInChildren<Text>().color = Opciones_Fuentes;

            PanelOpciones.transform.GetChild(0).transform.GetChild(0).GetComponent<Image>().color = Opciones_Selector;
            PanelOpciones.transform.GetChild(1).transform.GetChild(0).GetComponent<Image>().color = Opciones_Selector;
            PanelOpciones.transform.GetChild(2).transform.GetChild(0).GetComponent<Image>().color = Opciones_Selector;
            PanelOpciones.transform.GetChild(3).transform.GetChild(0).GetComponent<Image>().color = Opciones_Selector;
            PanelOpciones.transform.GetChild(4).transform.GetChild(0).GetComponent<Image>().color = Opciones_Selector;
            PanelOpciones.transform.GetChild(5).transform.GetChild(0).GetComponent<Image>().color = Opciones_Selector;
            PanelOpciones.transform.GetChild(6).transform.GetChild(0).GetComponent<Image>().color = Opciones_Selector;
            PanelOpciones.transform.GetChild(7).transform.GetChild(0).GetComponent<Image>().color = Opciones_Selector;

            // COLORES de las Opciones Avanzadas
            PanelOpcionesAvanzadas.GetComponent<Image>().color = OpcionesAvanzadas_Color;
            PanelOpcionesAvanzadas.transform.GetChild(0).GetComponentInChildren<Text>().color = OpcionesAvanzadas_Fuentes;
            PanelOpcionesAvanzadas.transform.GetChild(1).GetComponentInChildren<Text>().color = OpcionesAvanzadas_Fuentes;
            PanelOpcionesAvanzadas.transform.GetChild(2).GetComponentInChildren<Text>().color = OpcionesAvanzadas_Fuentes;
            PanelOpcionesAvanzadas.transform.GetChild(3).GetComponentInChildren<Text>().color = OpcionesAvanzadas_Fuentes;

            PanelOpcionesAvanzadas.transform.GetChild(1).transform.GetChild(0).GetComponent<Image>().color = OpcionesAvanzadas_Selector;
            PanelOpcionesAvanzadas.transform.GetChild(2).transform.GetChild(0).GetComponent<Image>().color = OpcionesAvanzadas_Selector;
            PanelOpcionesAvanzadas.transform.GetChild(3).transform.GetChild(0).GetComponent<Image>().color = OpcionesAvanzadas_Selector;

            // COLORES del Panel Copia y Pega
            PanelCopiando.GetComponent<Image>().color = CopiaPegaColor;
            PanelCopiando.transform.GetChild(1).GetComponent<Text>().color = CopiaPegaColorFont;
            PanelCopiando.transform.GetChild(4).GetComponentInChildren<Text>().color = CopiaPegaColorFont;
            PanelCopiando.transform.GetChild(2).GetComponentInChildren<Image>().color = CopiaPegaProgressBarBACK;
            PanelCopiando.transform.GetChild(2).transform.GetChild(1).GetComponentInChildren<Image>().color = CopiaPegaProgressBarFILL;
            PanelCopiando.transform.GetChild(3).GetComponentInChildren<Image>().color = CopiaPegaProgressBarBACK;
            PanelCopiando.transform.GetChild(3).transform.GetChild(1).GetComponentInChildren<Image>().color = CopiaPegaProgressBarFILL;
            PanelCopiando.transform.GetChild(3).transform.GetChild(2).GetComponent<Text>().color = CopiaPegaProgressBarFONT;

            // COLORES del Panel Overwirte
            PanelMensaje.transform.GetChild(0).GetComponent<Text>().color = ColorFontOverwrite;
            PanelMensaje.transform.GetChild(1).GetComponent<Text>().color = ColorFontOverwrite;
            PanelMensaje.transform.GetChild(2).GetComponent<Text>().color = ColorFontOverwrite;

            ColorBlock cb = new ColorBlock();
            cb.normalColor = ColorBotonesOverwrite;
            cb.highlightedColor = ColorBotonSelecionadoOverwrite;
            cb.pressedColor = ColorBotonesOverwrite;
            cb.disabledColor = ColorBotonesOverwrite;
            cb.colorMultiplier = 1;
            PanelMensaje.transform.GetChild(3).GetComponent<Button>().colors = cb;
            PanelMensaje.transform.GetChild(4).GetComponent<Button>().colors = cb;
            PanelMensaje.transform.GetChild(5).GetComponent<Button>().colors = cb;
            PanelMensaje.transform.GetChild(6).GetComponent<Button>().colors = cb;
            PanelMensaje.transform.GetChild(3).GetComponentInChildren<Text>().color = ColorBotonesFontOverwrite;
            PanelMensaje.transform.GetChild(4).GetComponentInChildren<Text>().color = ColorBotonesFontOverwrite;
            PanelMensaje.transform.GetChild(5).GetComponentInChildren<Text>().color = ColorBotonesFontOverwrite;
            PanelMensaje.transform.GetChild(6).GetComponentInChildren<Text>().color = ColorBotonesFontOverwrite;

            // COLORES del Panel Texto
            PanelEditorTexto2.GetComponent<Image>().color = ColorPanelTextos;
            PanelEditorTexto2.transform.GetChild(0).GetComponent<Image>().color = ColorPanelTextosInside;
            PanelEditorTexto2.transform.GetChild(0).GetComponentInChildren<Text>().color = ColorPanelTextosFont;

            // COLORES del Panel Descompactador
            PanelDescompactador.GetComponent<Image>().color = ColorPanelZipRar;
            PanelDescompactador.transform.GetChild(1).GetComponentInChildren<Text>().color = ColorPanelZipRarFont;
            PanelDescompactador.transform.GetChild(0).GetComponent<Image>().color = ColorPanelZipRarInside;
            PanelDescompactador.GetComponentInChildren<Text>().color = ColorPanelZipRarInsideFont;
            PanelDescompactador.transform.GetChild(2).GetComponentInChildren<Image>().color = ColorPanelProgresBACK;
            PanelDescompactador.transform.GetChild(2).transform.GetChild(1).GetComponentInChildren<Image>().color = ColorPanelProgresFILL;

            // COLORES del Panel PKG
            PanelPKG.GetComponent<Image>().color = ColorPanelPKG;
            PanelPKG.transform.GetChild(0).GetComponent<Image>().color = ColorPanelPKG_Inside;
            PanelPKG.transform.GetChild(0).GetComponentInChildren<Text>().color = ColorPanelPKG_Font;
            PanelPKG.transform.GetChild(2).GetComponent<Image>().color = ColorPanelPKG_Inside;
            PanelPKG.transform.GetChild(2).GetComponentInChildren<Text>().color = ColorPanelPKG_Font;
            PanelPKG.transform.GetChild(3).GetComponent<Image>().color = ColorPanelPKG_Inside;
            PanelPKG.transform.GetChild(3).GetComponentInChildren<Text>().color = ColorPanelPKG_Font;

            // COLORES del Scroll Bar de la Main Window
            scrollRect.gameObject.transform.GetChild(2).GetComponent<Image>().color = ColorScrollBarBACK_1;
            scrollRect.gameObject.transform.GetChild(2).transform.GetChild(0).GetComponentInChildren<Image>().color = ColorScrollBarHANDLE_1;

            // COLORES del Scroll Bar del Panel de Textos
            PanelEditorTexto1.transform.GetChild(0).transform.GetChild(1).GetComponent<Image>().color = ColorScrollBarBACK_2;
            PanelEditorTexto1.transform.GetChild(0).transform.GetChild(1).transform.GetChild(0).GetComponentInChildren<Image>().color = ColorScrollBarHANDLE_2;

            // COLORES del Scroll Bar del Panel Descompactador
            PanelDescompactador.transform.GetChild(0).transform.GetChild(1).GetComponent<Image>().color = ColorScrollBarBACK_3;
            PanelDescompactador.transform.GetChild(0).transform.GetChild(1).transform.GetChild(0).GetComponentInChildren<Image>().color = ColorScrollBarHANDLE_3;

            // Iconos de la Main Windows
            if (File.Exists(CaminoTemas + _s_ + "Icon1.png"))
                carpetasPrefab.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon1.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon2.png"))
                carpetasPrefabEspecial.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon2.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon3.png"))
                carpetasPrefabBlackList.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon3.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon4.png"))
                ficherosPrefab.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon4.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon5.png"))
                ficherosPrefabFOT.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon5.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon6.png"))
                ficherosPrefabMP3.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon6.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon7.png"))
                ficherosPrefabMP4.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon7.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon8.png"))
                ficherosPrefabTXT.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon8.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon9.png"))
                ficherosPrefabTHEME.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon9.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon10.png"))
                ficherosPrefabRAR.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon10.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon11.png"))
                ficherosPrefabPKG.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon11.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon12.png"))
                ficherosPrefabSFO.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon12.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon13.png"))
                textoOptions.GetComponentInParent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon13.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon26.png"))
                ficherosPrefabAVATAR.GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon26.png");

            // Iconos de las Opciones
            if (File.Exists(CaminoTemas + _s_ + "Icon14.png"))
                PanelOpciones.transform.GetChild(0).GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon14.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon27.png"))
                PanelOpciones.transform.GetChild(1).GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon27.png");



            if (File.Exists(CaminoTemas + _s_ + "Icon15.png"))
                PanelOpciones.transform.GetChild(3).GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon15.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon16.png"))
                PanelOpciones.transform.GetChild(4).GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon16.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon17.png"))
                PanelOpciones.transform.GetChild(5).GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon17.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon18.png"))
                PanelOpciones.transform.GetChild(6).GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon18.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon19.png"))
                PanelOpciones.transform.GetChild(7).GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon19.png");

            // Iconos de las Opciones Avanzadas
            if (File.Exists(CaminoTemas + _s_ + "Icon20.png"))
                PanelOpcionesAvanzadas.transform.GetChild(1).GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon20.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon21.png"))
                PanelOpcionesAvanzadas.transform.GetChild(2).GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon21.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon22.png"))
                PanelOpcionesAvanzadas.transform.GetChild(3).GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon22.png");

            // Iconos del Panel Copia y Pega
            if (File.Exists(CaminoTemas + _s_ + "Icon23.png"))
                PanelCopiando.transform.GetChild(0).GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon23.png");

            if (File.Exists(CaminoTemas + _s_ + "Icon24.png"))
                PanelCopiando.transform.GetChild(4).GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon24.png");

            // Icono del Descompactador
            if (File.Exists(CaminoTemas + _s_ + "Icon25.png"))
                PanelDescompactador.transform.GetChild(1).GetComponent<Image>().sprite = LeeImagenTema(CaminoTemas + _s_ + "Icon25.png");
        }
    }

    Sprite LeeImagenTema(string ImagenTema)
    {
        byte[] bytes = File.ReadAllBytes(ImagenTema);
        Texture2D texture = new Texture2D(0, 0, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Trilinear;
        texture.LoadImage(bytes);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.0f, 0.0f), 1.0f);
    }
}