namespace Chip8;

public class Cpu
{
    //Largura do display do Chip 8.
    private const int LARGURA_VIDEO = 64;
    //Altura do display do Chip 8.
    private const int ALTURA_VIDEO = 32;
    
    public byte[] registers = new byte[16]; //16 registradores de 1 byte.
    public byte[] memory = new byte[4096]; //4kb de memória.
    public ushort index; //Index usado em alguns opcodes.
    public ushort programCounter; //Program Counter, ou PC. Aponta para o endereço de memória que está usando.
    public ushort[] stack = new ushort[16]; //Pilha, usada para guardar instruções para uso posterior.
    public byte stackPointer; //Aponta para o endereço na pilha mais recente.
    public byte delayTimer; //Usado para alguns atrasos no sistema.
    public byte soundTimer; //Usado para reproduzir sons.
    public byte[] keypad = new byte[16]; //Guarda as teclas pressionadas.
    public uint[] display = new uint[LARGURA_VIDEO * ALTURA_VIDEO]; //Vetor de pixels do display.
    public ushort opcode; //Guarda a instrução atual.
    public byte[] fontes = //Guarda o sprite das fontes.
    {
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80  // F
    };
    public void Inicializar()
    {
        /*
         * Em hardware, todos os componentes guardam o valor 0 ou 1. Em software, variáveis
         * não inicializadas guardam um valor nulo. Nulo não existe em hardware, e consultas que podem
         * ser executadas esperando o valor 0 irão retornar erro. O método Inicializar define
         * todas as variáveis para 0, exceto o Program Counter que por padrão aponta para o endereço
         * hexadecimal 0x200. Os primeiros 0x1FF endereços, ou 512 bytes, são usados por padrão no sistema
         * para guardar informações de fonte ou alguns parâmetros de execução. Não afeta a emulação,
         * mas ROMs ainda esperam começar no endereço 0x200.
         */
        index = stackPointer = delayTimer = soundTimer = (byte)(opcode = 0);
        programCounter = 0x200;
        Array.Clear(registers, 0, registers.Length);
        for (int i = 0; i < memory.Length; i++)
        {
            memory[i] = 0;
        }
        Array.Clear(stack, 0, stack.Length);
        Array.Clear(keypad, 0, keypad.Length);
        Array.Clear(display, 0, display.Length);
        for (int i = 0; i < fontes.Length; i++)
        {
            memory[i] = fontes[i];
        }
    }
    
    /*
     * O Program Counter é normalmente incrementado por 1 a cada ciclo, mas como na nossa execução
     * lemos 2 endereços por vez, podemos incrementar ele por 2. O Chip 8 trabalha com números em
     * 8 bits, mas suas instruções são em 16 bits, então ele lê dois endereços por ciclo.
     */
    public void IncrementarProgramCounter() => programCounter += 2; 

    public void Executar(ushort opcode)
    {
        /*
         * Aqui pegamos a nossa instrução e interpretamos o que precisamos fazer com ela. A CPU recebe uma
         * instrução de 16 bits e ela utiliza os primeiros 4 bits, ou seja, o primeiro nibble para
         * definir o que deve ser executado.
         */
        this.opcode = opcode;
        ushort nibble = (ushort)((opcode & 0xF000) >> 12); //Isola os primeiros 4 bits.
        
        switch (nibble)
        {
            case 0x0: Opcode00(); break;
            case 0x1: Opcode01(); break;
            case 0x2: Opcode02(); break;
            case 0x3: Opcode03(); break;
            case 0x4: Opcode04(); break;
            case 0x5: Opcode05(); break;
            case 0x6: Opcode06(); break;
            case 0x7: Opcode07(); break;
            case 0x8: Opcode08(); break;
            case 0x9: Opcode09(); break;
            case 0xA: Opcode10(); break;
            case 0xB: Opcode11(); break;
            case 0xC: Opcode12(); break;
            case 0xD: Opcode13(); break;
            case 0xE: Opcode14(); break;
            case 0xF: Opcode15(); break;
            default: Console.WriteLine("Opcode desconhecido."); break;
        }
    }
    private void Opcode00()
    {
        switch (opcode)
        {
            //Limpa o display.
            case 0x00E0: Array.Clear(display, 0, display.Length); break;
            //Retorna o programa de uma subrotina pegando um Program Counter armazenado na pilha.
            case 0x00EE:
                stackPointer--;
                programCounter = stack[stackPointer];
                break;
        }

    }

    private void Opcode01()
    {
        //Define o Program Counter um valor informado no Opcode.
        programCounter = (ushort)(opcode & 0x0FFF);
    }
    private void Opcode02()
    {
        //Começa uma subrotina guardando o Program Counter na pilha.
        stack[stackPointer] = programCounter;
        stackPointer++;
        programCounter = (ushort)(opcode & 0x0FFF);
    }

    private void Opcode03()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte kk = (byte)(opcode & 0x00FF); //Isola o terceiro e quarto nibble.
        /*
         * Se o valor nos registradores na posição X for igual ao valor
         * de KK, incrementa o PC.
         */
        if (registers[vx] == kk) programCounter += 2;
 
    }

    private void Opcode04()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte kk = (byte)(opcode & 0x00FF); //Isola o terceiro e quarto nibble.
        /*
         * Se o valor nos registradores na posição X for diferente do valor
         * de KK, incrementa o PC.
         */
        if (registers[vx] != kk) programCounter += 2;
        
    }

    private void Opcode05()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte vy = (byte)((opcode & 0x00F0) >> 4); //Isola o terceiro nibble.
        /*
         * Se o valor nos registradores na posição X for igual
         * ao valor no registrador na posição Y, incrementa o PC.
         */
        if (registers[vx] == registers[vy]) programCounter += 2;
        
    }
        
    private void Opcode06()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte kk = (byte)(opcode & 0x00FF); //Isola o terceiro e quarto nibble.
        //Armazena o terceiro e quarto nibble no registrador X.
        registers[vx] = kk;
        
    }

    private void Opcode07()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte kk = (byte)(opcode & 0x00FF); //Isola o terceiro e quarto nibble.
        //Soma KK nos registradores.
        registers[vx] += kk;
        
    }

    private void Opcode08()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte vy = (byte)((opcode & 0x00F0) >> 4); //Isola o terceiro nibble.
        byte operation = (byte)(opcode & 0x000F); //O quarto nibble define a operação a ser realizada.
        switch (operation)
        {
            //Registrador X recebe valor no registrador Y.
            case 0: registers[vx] = registers[vy]; break; 
            //Registrador X recebe operação lógica OU do valor no registrador Y.
            case 1: registers[vx] |= registers[vy]; registers[15] = 0; break;
            //Registrador X recebe operação lógica E do valor no registrador Y.
            case 2: registers[vx] &= registers[vy]; registers[15] = 0; break;
            //Registrador X recebe operação lógica OU EXCLUSIVO do valor no registrador Y.
            case 3: registers[vx] ^= registers[vy]; registers[15] = 0; break;
            case 4:
                //Registrador X recebe o valor da soma com registrador Y.
                ushort sum = (ushort)(registers[vx] + registers[vy]);
                /*
                 * Se o valor da soma for maior que 255, o bit de overflow deve ser definido.
                 * Este bit fica na posição 15 dos registradores.
                 */
                registers[15] = (byte)(sum > 255 ? 1 : 0);
                /*
                 * A soma então deve ser operada com a operação 
                 * lógica E para garantir que o valor seja no máximo 255.
                 */
                registers[vx] = (byte)(sum & 0xFF);
                /*
                 * OBSERVAÇÃO: O bit de overflow é apenas um indicativo, e no Chip 8 não afeta a soma.
                 * Em outros emuladores como de NES é necessário somar o bit de overflow na conta.
                 */
                break;
            case 5:
                /*
                 * Se o valor da subtração for menor que 0, o bit de underflow deve ser definido.
                 * Este bit fica na posição 15 dos registradores.
                 */
                registers[15] = (byte)(registers[vx] >= registers[vy] ? 1 : 0);
                //Realiza a subtração.
                registers[vx] -= registers[vy];
                /*
                 * OBSERVAÇÃO: O bit de underflow é apenas um indicativo, e no Chip 8 não afeta a subtração.
                 * Em outros emuladores como de NES é necessário somar o bit de underflow na conta.
                 */
                break;
            case 6:
                //Isola o bit menos significativo e guarda no Registrador 15.
                registers[15] = (byte)(registers[vx] & 0x1);
                /*
                 * Desloca a posição de todos os bits uma posição para a direita.
                 * Efetivamente serve como uma divisão rápida por 2.
                 */
                registers[vx] = registers[vy] >>= 1;
                break;
            case 7:
                /*
                 * Se o valor da subtração for menor que 0, o bit de underflow deve ser definido.
                 * Este bit fica na posição 15 dos registradores.
                 */
                registers[15] = (byte)(registers[vy] >= registers[vx] ? 1 : 0);
                /*
                 * Realiza uma subtração.
                 * Diferente do caso 5, aqui subtraímos X de Y, depois guardamos o resultado em X.
                 */
                registers[vx] = (byte)((registers[vy] - registers[vx]) & 0xFF);
                /*
                 * OBSERVAÇÃO: O bit de underflow é apenas um indicativo, e no Chip 8 não afeta a subtração.
                 * Em outros emuladores como de NES é necessário somar o bit de underflow na conta.
                 */
                break;
            case 0xE:
                //Isola o bit mais significativo e guarda no Registrador 15.
                registers[15] = (byte)((registers[vx] & 0x80) >> 7);
                /*
                 * Desloca a posição de todos os bits uma posição para a esquerda.
                 * Efetivamente serve como uma multiplicação rápida por 2.
                 */
                registers[vx] = registers[vy] <<= 1;
                break;
        }
        
    }
    private void Opcode09()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte vy = (byte)((opcode & 0x00F0) >> 4); //Isola o terceiro nibble.
        //Se o registrador X for diferente do registrador Y, incrementa o PC.
        if (registers[vx] != registers[vy]) programCounter += 2;
        
    }

    private void Opcode10()
    {
        //Index recebe o valor indicado no opcode.
        index = (ushort)(opcode & 0x0FFF);
    }

    private void Opcode11()
    {
        //PC é definido com a soma do registrador 0 mais o valor indicado no opcode.
        programCounter = (ushort)(registers[0] + (opcode & 0x0FFF));
    }
    private void Opcode12()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte kk = (byte)(opcode & 0x00FF); //Isola o terceiro e quarto nibble.
        Random random = new Random();
        /*
         * Registrador X recebe um valor aleatório entre 0 e 255.
         * É então realizado uma operação lógica E com esse valor e KK para definir o número aleatório.
         */ 
        registers[vx] = (byte)(random.Next(0, 255) & kk);
       
    }

    private void Opcode13()
    {
        byte vx = registers[(opcode & 0x0F00) >> 8]; //Isola o segundo nibble (registrador X).
        byte vy = registers[(opcode & 0x00F0) >> 4]; //Isola o terceiro nibble (registrador Y).
        byte altura = (byte)(opcode & 0x000F); //Isola o quarto nibble (altura do sprite).
        //Aqui o registrador 15 é a flag de colisão. Serve para verificar se 2 pixels colidiram.
        registers[15] = 0;
        //Percorre cada linha da altura do sprite.
        for (int y = 0; y < altura; y++)
        {
            //O byte da sprite é informado a partir da posição no Index mais a altura.
            byte spriteByte = memory[index + y];
            //Percorre os bits no byte da sprite.
            for (int x = 0; x < 8; x++)
            {
                //Se o bit específico no byte for 1, começa a desenhar.
                if ((spriteByte & (0x80 >> x)) != 0) 
                {
                    //Guarda a posição X do pixel.
                    int posX = (vx + x) % LARGURA_VIDEO;
                    //Guarda a posição Y do pixel.
                    int posY = (vy + y) % ALTURA_VIDEO;
                    //Salva a posição no vetor Display do pixel.
                    int pixelIndex = posX + posY * LARGURA_VIDEO;
                    //Se o display na posição do pixelIndex já for 1, houve uma colisão de pixels.
                    if (display[pixelIndex] == 1)
                    {
                        registers[15] = 1; 
                    }
                    /*
                     * Alterna o valor do pixel na tela, podendo efetivamente usar este método
                     * para acender ou apagar um determinado pixel na tela.
                     */ 
                    display[pixelIndex] ^= 1; 
                }
            }
        }
    }
    private void Opcode14()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte keyCode = (byte)(opcode & 0x00FF); //Isola o terceiro e quarto nibble.
        //Verifica se alguma tecla está ou não pressionada. Pode incrementar o PC.
        if (keyCode == 0x9E && keypad[registers[vx]] != 0) programCounter += 2;
        if (keyCode == 0xA1 && keypad[registers[vx]] == 0) programCounter += 2;
        
    }

    private void Opcode15()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte op = (byte)(opcode & 0x00FF); //Isola o terceiro e quarto nibble.
        switch (op)
        {
            //Registrador X recebe o valor do delayTimer. 
            case 0x07: registers[vx] = delayTimer; break;
            //Percorre o keypad para verificar se alguma tecla está pressionada.
            case 0x0A:
                for (byte i = 0; i < 16; i++) 
                {
                    if (keypad[i] != 0) 
                    {
                        //Se estiver, guarda qual tecla está no registrador X.
                        registers[vx] = i;
                        return;
                    }
                }
                programCounter -= 2; // Se nenhuma tecla foi pressionada, retrocede o PC
                break;
            //Define o delay timer.
            case 0x15: delayTimer = registers[vx]; break;
            //Define o sound timer.
            case 0x18: soundTimer = registers[vx]; break;
            //Define o index.
            case 0x1E: index += registers[vx]; break;
            //Define o index multiplicado por 5.
            case 0x29: index = (ushort)(registers[vx] * 5); break;
            //Separa um valor em binário em seus dígitos em decimal. Útil para exibir pontuação na tela.
            case 0x33:
                memory[index] = (byte)(registers[vx] / 100);
                memory[index + 1] = (byte)((registers[vx] / 10) % 10);
                memory[index + 2] = (byte)(registers[vx] % 10);
                break;
            //Armazena os registradores na memória.
            case 0x55:
                for (byte i = 0; i <= vx; i++) memory[index + i] = registers[i];
                break;
            //Armazena a memória nos registradores.
            case 0x65:
                for (byte i = 0; i <= vx; i++) registers[i] = memory[index + i];
                break;
        }
    }
}