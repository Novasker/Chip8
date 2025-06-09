# Emulador Chip-8

Este projeto é um **emulador funcional do Chip-8**, desenvolvido em **C# com WPF**. 
O objetivo principal foi explorar conceitos fundamentais de arquitetura de CPUs, instruções de baixo nível e manipulação gráfica, através
da construção de um sistema que interpreta e executa programas escritos originalmente para o interpretador Chip-8.

## 🎮 Controles do Teclado

O Chip-8 usa um teclado hexadecimal de 16 teclas, mapeados no emulador por padrão da seguinte forma:
```
┌───┬───┬───┬───┐     ┌───┬───┬───┬───┐
│ 1 │ 2 │ 3 │ C │     │ 1 │ 2 │ 3 │ 4 │
├───┼───┼───┼───┤     ├───┼───┼───┼───┤
│ 4 │ 5 │ 6 │ D │     │ Q │ W │ E │ R │
├───┼───┼───┼───┤ --> ├───┼───┼───┼───┤
│ 7 │ 8 │ 9 │ E │     │ A │ S │ D │ F │
├───┼───┼───┼───┤     ├───┼───┼───┼───┤
│ A │ 0 │ B │ F │     │ Z │ X │ C │ V │
└───┴───┴───┴───┘     └───┴───┴───┴───┘
```

## 🧠 Funcionalidades

- **Emulação completa** das instruções do Chip-8.
- **Interface gráfica** usando WPF com renderização via `Image` e manipulação de pixels.
- **Simulação de clock** via `DispatcherTimer`.
- Exibição visual dos **64x32 pixels** do display do Chip-8.

## 📚 Sobre o Chip-8
Chip-8 é uma linguagem de máquina interpretada, originalmente criada nos anos 70, usada para facilitar o desenvolvimento de jogos em microcomputadores de 8 bits.
Emular o Chip-8 é um projeto clássico para quem estuda arquitetura de computadores ou deseja aprender como uma CPU funciona em nível mais próximo do hardware.
