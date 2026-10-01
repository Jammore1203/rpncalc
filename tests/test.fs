\ Unit tests for rpnlib.fs.  Run: gforth tests/test.fs
require ../rpnlib.fs

variable failures
variable stack-before

: start ( -- )  depth stack-before ! ;
: check ( got expected "name" -- )
    parse-name 2>r
    depth stack-before @ - 2 <> if
        ." FAIL  " 2r> type ."  (left the stack unbalanced)" cr
        1 failures +! clear exit then
    = if ." PASS  " else ." FAIL  " 1 failures +! then
    2r> type cr ;

\ Runs a line through the token interpreter, as the CLI does.
: calc ( a u -- n )  evaluate-line ;

start 0xff 255                         check hex-literal
start s" 0b1010_0101" calc 165         check binary-literal-with-separator
start s" 0o755" calc 493               check octal-literal
start s" 1_000_000" calc 1000000       check decimal-separator
start s" -0x10" calc -16               check negative-hex
start s" 0xdead_beef" calc 3735928559  check hex-separator
start s" 2 3 + 10 *" calc 50           check rpn-arithmetic
start s" 4 GiB" calc 4294967296        check gibibytes
start s" 3 MB" calc 3000000            check megabytes
start s" 2 hrs" calc 7200000           check hours-to-ms
start s" 0b1001 1 setbit" calc 11      check setbit
start s" 0b1011 0 clrbit" calc 10      check clrbit
start s" 0b1011 3 flipbit" calc 3      check flipbit
start s" 0b1011 3 bit" calc 1          check bit-test
start s" 0xff popcount" calc 8         check popcount
start s" -1 popcount" calc 64          check popcount-all
start s" 12 mask" calc 4095            check mask
start s" 64 mask" calc -1              check mask-64
start s" 0x12345678 bswap32" calc 0x78563412 check bswap32
start s" 0x1234 bswap16" calc 0x3412   check bswap16
start s" 1 clz" calc 63                check clz
start s" 0 clz" calc 64                check clz-zero
start s" 8 ctz" calc 3                 check ctz
start s" 0x8000000000000001 1 rol" calc 3 check rol-wraps
start s" 3 1 ror" calc 0x8000000000000001 check ror-wraps
start s" -16 2 sar" calc -4            check arithmetic-shift
start s" 2 10 **" calc 1024            check power
start s" 48 180 gcd" calc 12           check gcd
start s" 4 6 lcm" calc 12              check lcm
start s" 1000 4096 align" calc 4096    check align-up
start s" 8192 4096 align" calc 8192    check align-exact
start s" 250 20 pct" calc 50           check percent
start s" 1 2 3 4 sum" calc 10          check sum

\ errors are caught, reported and leave a usable interpreter
start s" 1 0 /" calc failed @ true     check division-by-zero-caught
start s" nonsense" calc failed @ true  check unknown-word-caught
start s" 0b102" calc failed @ true     check bad-binary-digit-caught
start s" 5 1+" calc 6                  check still-works-after-errors

: summary ( -- )
    cr failures @ ?dup 0= if ." All rpnlib tests passed." cr bye then
    . ." test(s) FAILED" cr 1 (bye) ;
summary
