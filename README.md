# rpncalc

![CI](https://github.com/Jammore1203/rpncalc/actions/workflows/ci.yml/badge.svg)

A programmer's RPN calculator, written in Forth. Every result is shown in decimal, hex, octal and nibble-grouped binary at once, and there are words for the bit-twiddling, byte-swapping and unit conversions you'd otherwise do on a scrap of paper.

```console
$ rpncalc 0xff 2 shl
dec 1020  hex 0x3fc  oct 0o1774  bin 0b0011_1111_1100

$ rpncalc 0x12345678 bswap32
dec 2018915346  hex 0x78563412  oct 0o17025432022  bin 0b0111_1000_0101_0110_0011_0100_0001_0010

$ rpncalc 4 GiB
dec 4294967296  hex 0x100000000  oct 0o40000000000  bin 0b0001_0000_0000_0000_0000_0000_0000_0000_0000

$ rpncalc -1
dec -1 (unsigned 18446744073709551615)  hex 0xffffffffffffffff  ...

$ rpncalc 65
dec 65  hex 0x41  oct 0o101  bin 0b0100_0001  char 'A'
```

Run it with no arguments for an interactive session. The stack carries over between lines:

```console
$ rpncalc
> 1000 4096 align
dec 4096  hex 0x1000  oct 0o10000  bin 0b0001_0000_0000_0000
> 3 *
dec 12288  hex 0x3000  oct 0o30000  bin 0b0011_0000_0000_0000
```

## Words

| | |
|---|---|
| **numbers** | `42` `-7` `0xff` `0b1010` `0o755`, with `_` separators: `1_000_000` `0xdead_beef` |
| **math** | `+ - * / mod ** gcd lcm abs negate min max pct align` |
| **bits** | `and or xor invert shl shr sar rol ror popcount clz ctz` |
| | `bit setbit clrbit flipbit mask bswap16 bswap32 bswap64` |
| **units** | `KiB MiB GiB TiB` · `KB MB GB` · `sec mins hrs days` (to milliseconds) |
| **stack** | `dup drop swap over rot clear sum` |

It's a real Forth underneath, so every standard Forth word works too.

Errors are caught and reported without losing the session:

```console
> 1 0 /
division by zero: /
> 0b102
unknown word or bad number: 0b102
```

In one-shot mode an error exits with status 1, so it can be used in scripts.

## Install

Needs [gforth](https://gforth.org) (`apt install gforth`, `brew install gforth`) on a 64-bit machine.

```bash
./bin/rpncalc help
make test       # 37 unit tests + CLI golden test
make install    # symlinks into PREFIX/bin
```

## How it works

Forth reads postfix notation natively, so most of the calculator is ordinary Forth words: `: KiB 1024 * ;` is the whole kibibyte feature.

The interesting part is the small **token interpreter** in front of Forth's own. Each token that looks like a number gets two extra treatments before reaching Forth:

1. `_` digit separators are removed.
2. `0b` / `0o` prefixes are parsed by hand. Gforth 0.7 understands `0x` but not these.

Everything else is passed to `evaluate`. The interpreter keeps its own state (the current line and token) in variables rather than on the data stack, because the data stack *is* the calculator's stack. A line runs under `catch`, so a bad token gets reported (with its name), the stack is restored, and the session continues.

`rol`/`ror` mask the shift count to 6 bits, as x86 does, and `.bin` sizes its output to the highest set bit, rounded up to whole nibbles.
