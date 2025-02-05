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
            case 0x0: Opcode0nnn(); break;
            case 0x1: Opcode1nnn(); break;
            case 0x2: Opcode2nnn(); break;
            case 0x3: Opcode3xkk(); break;
            case 0x4: Opcode4xkk(); break;
            case 0x5: Opcode5xy0(); break;
            case 0x6: Opcode6xkk(); break;
            case 0x7: Opcode7xkk(); break;
            case 0x8: Opcode8xyn(); break;
            case 0x9: Opcode9xy0(); break;
            case 0xA: OpcodeAnnn(); break;
            case 0xB: OpcodeBnnn(); break;
            case 0xC: OpcodeCxkk(); break;
            case 0xD: OpcodeDxyn(); break;
            case 0xE: OpcodeExyn(); break;
            case 0xF: OpcodeFxkk(); break;
            default: Console.WriteLine("Opcode desconhecido."); break;
        }
    }
    private void Opcode0nnn()
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

    private void Opcode1nnn()
    {
        //Define o Program Counter um valor informado no Opcode.
        programCounter = (ushort)(opcode & 0x0FFF);
    }
    private void Opcode2nnn()
    {
        //Começa uma subrotina guardando o Program Counter na pilha.
        stack[stackPointer] = programCounter;
        stackPointer++;
        programCounter = (ushort)(opcode & 0x0FFF);
    }

    private void Opcode3xkk()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte kk = (byte)(opcode & 0x00FF); //Isola o terceiro e quarto nibble.
        /*
         * Se o valor nos registradores na posição X for igual ao valor
         * de KK, incrementa o PC.
         */
        if (registers[vx] == kk) programCounter += 2;
 
    }

    private void Opcode4xkk()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte kk = (byte)(opcode & 0x00FF); //Isola o terceiro e quarto nibble.
        /*
         * Se o valor nos registradores na posição X for diferente do valor
         * de KK, incrementa o PC.
         */
        if (registers[vx] != kk) programCounter += 2;
        
    }

    private void Opcode5xy0()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte vy = (byte)((opcode & 0x00F0) >> 4); //Isola o terceiro nibble.
        /*
         * Se o valor nos registradores na posição X for igual
         * ao valor no registrador na posição Y, incrementa o PC.
         */
        if (registers[vx] == registers[vy]) programCounter += 2;
        
    }
        
    private void Opcode6xkk()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte kk = (byte)(opcode & 0x00FF); //Isola o terceiro e quarto nibble.
        //Armazena o terceiro e quarto nibble no registrador X.
        registers[vx] = kk;
        
    }

    private void Opcode7xkk()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte kk = (byte)(opcode & 0x00FF); //Isola o terceiro e quarto nibble.
        //Soma KK nos registradores.
        registers[vx] += kk;
        
    }

    private void Opcode8xyn()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte vy = (byte)((opcode & 0x00F0) >> 4); //Isola o terceiro nibble.
        byte operation = (byte)(opcode & 0x000F); //O quarto nibble define a operação a ser realizada.
        byte x;
        byte y;
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
                x = registers[vx]; 
                y = registers[vy];
                registers[vx] = (byte)(x + y & 0xFF);
                registers[15] = (byte)(x + y > 255 ? 1 : 0);
                break;
            case 5:
                x = registers[vx];
                y = registers[vy];
                registers[vx] = (byte)(x - y & 0xFF);
                registers[15] = (byte)(x >= y ? 1 : 0);
                break;
            case 6:
                x = registers[vx];
                y = registers[vy];
                registers[vx] = (byte)(y >> 1);
                registers[15] = (byte)(x & 0x1);
                break;
            case 7:
                x = registers[vx];
                y = registers[vy];
                registers[vx] = (byte)(y - x & 0xFF);
                registers[15] = (byte)(y >= x ? 1 : 0);
                break;
            case 0xE:
                x = registers[vx];
                y = registers[vy];
                registers[vx] = (byte)(y << 1);
                registers[15] = (byte)((x & 0x80) >> 7);
                break;
        }
        
    }
    private void Opcode9xy0()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte vy = (byte)((opcode & 0x00F0) >> 4); //Isola o terceiro nibble.
        //Se o registrador X for diferente do registrador Y, incrementa o PC.
        if (registers[vx] != registers[vy]) programCounter += 2;
        
    }

    private void OpcodeAnnn()
    {
        //Index recebe o valor indicado no opcode.
        index = (ushort)(opcode & 0x0FFF);
    }

    private void OpcodeBnnn()
    {
        //PC é definido com a soma do registrador 0 mais o valor indicado no opcode.
        programCounter = (ushort)(registers[0] + (opcode & 0x0FFF));
    }
    private void OpcodeCxkk()
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

    private void OpcodeDxyn()
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
    private void OpcodeExyn()
    {
        byte vx = (byte)((opcode & 0x0F00) >> 8); //Isola o segundo nibble.
        byte keyCode = (byte)(opcode & 0x00FF); //Isola o terceiro e quarto nibble.
        //Verifica se alguma tecla está ou não pressionada. Pode incrementar o PC.
        if (keyCode == 0x9E && keypad[registers[vx]] != 0) programCounter += 2;
        if (keyCode == 0xA1 && keypad[registers[vx]] == 0) programCounter += 2;
        
    }

    private void OpcodeFxkk()
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