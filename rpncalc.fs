\ rpncalc.fs - command-line front end. Run via bin/rpncalc.
require rpnlib.fs

: help ( -- )
    ." rpncalc - programmer's RPN calculator" cr cr
    ." usage: rpncalc EXPRESSION...    evaluate and print the result" cr
    ."        rpncalc                  interactive (reads lines from stdin)" cr cr
    ." numbers   42  -7  0xff  0b1010  0o755  1_000_000  0xdead_beef" cr
    ." math      + - * / mod ** gcd lcm abs negate min max pct align" cr
    ." bits      and or xor invert shl shr sar rol ror popcount clz ctz" cr
    ."           bit setbit clrbit flipbit mask bswap16 bswap32 bswap64" cr
    ." units     KiB MiB GiB TiB  KB MB GB  sec mins hrs days (-> ms)" cr
    ." stack     dup drop swap over rot clear sum" cr ;

create line-buf 1024 allot
create arg-buf 4096 allot
variable arg-len

: show ( ... -- ... )
    depth 0= if exit then
    depth 1 > if ." stack: " depth 0 do depth i - 1- pick . loop cr then
    dup .all cr ;

: prompt ( -- )  s" > " stderr write-file throw ;

: repl ( -- )
    begin
        prompt
        line-buf 1024 stdin read-line throw
    while
        line-buf swap evaluate-line show
    repeat drop ;

: append-arg ( a u -- )
    tuck arg-buf arg-len @ + swap move arg-len +!
    bl arg-buf arg-len @ + c! 1 arg-len +! ;

: help-arg? ( a u -- f )
    2dup s" -h" compare 0= >r s" --help" compare 0= r> or ;

: main ( -- )
    argc @ 1 > if 1 arg help-arg? if help bye then then
    0 arg-len !
    begin next-arg dup while append-arg repeat 2drop
    arg-len @ if
        arg-buf arg-len @ evaluate-line
        failed @ if 1 (bye) then
        show
    else repl then ;

main bye
