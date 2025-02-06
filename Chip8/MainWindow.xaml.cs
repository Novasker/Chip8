using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Chip8;

public partial class MainWindow : Window
{
    public WriteableBitmap bitmap;
    public const int LARGURA_TELA = 64;
    public const int ALTURA_TELA = 32;
    //Cria um relógio de 1ms.
    DispatcherTimer cpuTimer = new DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(1) 
    };
    //Cria um segundo relógio para os frames.
    DispatcherTimer drawTimer = new DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(16)
    };
    private Cpu cpu;
    
    public MainWindow()
    {   
        //Define alguns parâmetros de inicialização da janela.
        InitializeComponent();
        bitmap = new WriteableBitmap(LARGURA_TELA, ALTURA_TELA, 96, 96, PixelFormats.Pbgra32, null);
        Image.Source = bitmap;
        RenderOptions.SetBitmapScalingMode(Image, BitmapScalingMode.NearestNeighbor);
    }
    private void CarregarRom(object sender, RoutedEventArgs e)
    {
        //Abre uma janela no Explorador de Arquivos buscando arquivos .ch8, as roms do emulador
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Arquivos ROM (*.ch8)|*.ch8|Todos os Arquivos (*.*)|*.*"
        };
        /*
         * Pede para confirmar, pega o caminho do arquivo e lê como um vetor de bytes,
         * depois manda este arquivo para iniciar a execução.
         */
        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;
            try
            {
                byte[] romBytes = File.ReadAllBytes(filePath);
                Inicializar(romBytes);
            }
            catch (Exception ex)
            {
                // ignored
            }
        }
    }
    public void Inicializar(byte[] rom)
    {
        cpu = new Cpu();
        //Inicializa as variáveis da CPU.
        cpu.Inicializar();
        /*
         * Copia a ROM para a Memória RAM da CPU. Programas começam a ser lidos na posição
         * 0x200 pois há um overhead de instruções no Chip8 nas primeiras posições.
         */
        for (int i = 0x200; i < rom.Length + 0x200; i++)
        {
            cpu.memory[i] = rom[i - 0x200];
        }
        //Para o timer.
        cpuTimer.Stop();
        //Limpa o timer se for um novo ciclo de rom.
        cpuTimer = null;
        //Cria o timer novamente.
        cpuTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1)
        };
        //Atribui o clock ao timer.
        cpuTimer.Tick += (s, args) => Clock();
        //Inicia o timer.
        cpuTimer.Start();
        //Para o timer.
        drawTimer.Stop();
        //Limpa o timer se for um novo ciclo de rom.
        drawTimer = null;
        //Cria o timer novamente.
        drawTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        //Atribui o render ao timer.
        drawTimer.Tick += (s, args) => Render();
        //Inicia o timer.
        drawTimer.Start();
    }

    private void ProcessarCiclo()
    {
        /*
         * Processo de execução do emulador por ciclo. Começa criando um número de 16 bits com a instrução.
         * O processador do Chip8 manipula apenas 8 bits, mas suas instruções são em 16. Portanto, é
         * necessário concatenar dois espaços de memória para formar uma instrução.
         */
        ushort opcode = (ushort)((cpu.memory[cpu.programCounter] << 8) | cpu.memory[cpu.programCounter + 1]);
        cpu.IncrementarProgramCounter();
        Dispatcher.Invoke(LerTeclas);
        // Processa o opcode atual.
        cpu.Executar(opcode);
    }

    private void Sair(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
    
    //Lê o vetor de pixels e desenha na tela
    public void DesenharFrame(uint[] display)
    {
        bitmap.Lock();
        for (int y = 0; y < ALTURA_TELA; y++)
        {
            for (int x = 0; x < LARGURA_TELA; x++)
            {
                int index = y * LARGURA_TELA + x; // índice do pixel na tela
                uint cor = display[index] != 0 ? 0xFFFFFFFF : 0xFF000000; // branco ou preto
                IntPtr pBackBuffer = bitmap.BackBuffer + y * bitmap.BackBufferStride + x * 4;
                System.Runtime.InteropServices.Marshal.WriteInt32(pBackBuffer, (int)cor);
            }
        }
        bitmap.AddDirtyRect(new Int32Rect(0, 0, LARGURA_TELA, ALTURA_TELA));
        bitmap.Unlock();
    }
    public void LerTeclas()
    {
        cpu.keypad[1] = (byte)(Keyboard.IsKeyDown(Key.D1) ? 1 : 0);  // 1 -> 1
        cpu.keypad[2] = (byte)(Keyboard.IsKeyDown(Key.D2) ? 1 : 0);  // 2 -> 2
        cpu.keypad[3] = (byte)(Keyboard.IsKeyDown(Key.D3) ? 1 : 0);  // 3 -> 3
        cpu.keypad[12] = (byte)(Keyboard.IsKeyDown(Key.D4) ? 1 : 0); // 4 -> C
        cpu.keypad[4] = (byte)(Keyboard.IsKeyDown(Key.Q) ? 1 : 0);   // Q -> 4
        cpu.keypad[5] = (byte)(Keyboard.IsKeyDown(Key.W) ? 1 : 0);   // W -> 5
        cpu.keypad[6] = (byte)(Keyboard.IsKeyDown(Key.E) ? 1 : 0);   // E -> 6
        cpu.keypad[13] = (byte)(Keyboard.IsKeyDown(Key.R) ? 1 : 0);  // R -> D
        cpu.keypad[7] = (byte)(Keyboard.IsKeyDown(Key.A) ? 1 : 0);   // A -> 7
        cpu.keypad[8] = (byte)(Keyboard.IsKeyDown(Key.S) ? 1 : 0);   // S -> 8
        cpu.keypad[9] = (byte)(Keyboard.IsKeyDown(Key.D) ? 1 : 0);   // D -> 9
        cpu.keypad[14] = (byte)(Keyboard.IsKeyDown(Key.F) ? 1 : 0);  // F -> E
        cpu.keypad[10] = (byte)(Keyboard.IsKeyDown(Key.Z) ? 1 : 0);  // Z -> A
        cpu.keypad[0] = (byte)(Keyboard.IsKeyDown(Key.X) ? 1 : 0);   // X -> 0
        cpu.keypad[11] = (byte)(Keyboard.IsKeyDown(Key.C) ? 1 : 0);  // C -> B
        cpu.keypad[15] = (byte)(Keyboard.IsKeyDown(Key.V) ? 1 : 0);  // V -> F
    }
    private void Clock()
    {
        //Faz processar o ciclo 5 vezes por 1ms.
        for (int i = 0; i < 5; i++)
        {
            ProcessarCiclo();
        }
    }

    private void Render()
    {
        DesenharFrame(cpu.display);
        //Os timers são reduzidos numa frequência de 60hz, então aproveito o timer de frames.
        if (cpu.delayTimer > 0) cpu.delayTimer--;
        if (cpu.soundTimer > 0)
        {
            //Cada bipe dura 1 frame.
            Console.Beep(440,16);
            cpu.soundTimer--;
        }
    }
}
