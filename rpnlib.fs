\ rpnlib.fs - the words behind rpncalc, a programmer's RPN calculator.
\ Assumes 64-bit cells (gforth on any 64-bit platform).

\ Gforth is case-insensitive, so unit names like TiB and GB shadow system
\ words (tib, gb). Code compiled earlier keeps the originals, so that's
\ harmless; silence the warnings.
warnings off

\ ---- bit twiddling ---------------------------------------------------

: shl ( u n -- u' )  lshift ;
: shr ( u n -- u' )  rshift ;
: sar ( n u -- n' )  0 ?do 2/ loop ;           \ arithmetic shift right
: rol ( u n -- u' )  63 and 2dup lshift -rot 64 swap - rshift or ;
: ror ( u n -- u' )  63 and 64 swap - 63 and rol ;
: popcount ( u -- n )
    0 swap begin dup while dup 1 and rot + swap 1 rshift repeat drop ;
: bit     ( u i -- 0|1 )  rshift 1 and ;
: setbit  ( u i -- u' )   1 swap lshift or ;
: clrbit  ( u i -- u' )   1 swap lshift invert and ;
: flipbit ( u i -- u' )   1 swap lshift xor ;
: mask    ( n -- 2^n-1 )  dup 64 >= if drop -1 else 1 swap lshift 1- then ;
: bswap ( u nbytes -- u' )                     \ reverse the low nbytes bytes
    0 rot rot 0 ?do dup 255 and rot 8 lshift or swap 8 rshift loop drop ;
: bswap16 ( u -- u' )  2 bswap ;
: bswap32 ( u -- u' )  4 bswap ;
: bswap64 ( u -- u' )  8 bswap ;
: clz ( u -- n )  64 swap begin dup while 1 rshift swap 1- swap repeat drop ;
: ctz ( u -- n )
    dup 0= if drop 64 exit then
    0 swap begin dup 1 and 0= while 1 rshift swap 1+ swap repeat drop ;

\ ---- arithmetic ------------------------------------------------------

: **  ( b e -- b^e )  1 swap 0 ?do over * loop nip ;
: gcd ( a b -- g )    begin dup while tuck mod repeat drop abs ;
: lcm ( a b -- l )    2dup * abs -rot gcd / ;
: align ( n a -- n' ) tuck 1- + over / * ;     \ round n up to a multiple of a
: pct ( n p -- n*p/100 )  100 */ ;

\ ---- units -----------------------------------------------------------

: KiB  1024 * ;
: MiB  1024 * 1024 * ;
: GiB  1024 * 1024 * 1024 * ;
: TiB  1024 * 1024 * 1024 * 1024 * ;
: KB   1000 * ;
: MB   1000000 * ;
: GB   1000000000 * ;
: sec  1000 * ;                                 \ seconds -> ms
: mins 60000 * ;
: hrs  3600000 * ;
: days 86400000 * ;

\ ---- stack -----------------------------------------------------------

: clear ( ... -- )  depth 0 ?do drop loop ;
: sum ( ... -- n )  depth 0= if 0 else depth 1- 0 ?do + loop then ;

\ ---- output ----------------------------------------------------------

: digit>char ( n -- c )  dup 10 < if [char] 0 + else 10 - [char] a + then ;

: u.base ( u base -- )                          \ unsigned, lowercase digits
    >r 0 r@ um/mod ?dup if r@ recurse then digit>char emit r> drop ;

: bits-needed ( u -- n )  64 swap clz - 4 max 3 + -4 and ;

: .bin ( u -- )                                 \ 0b1111_0000, nibble grouped
    ." 0b" dup bits-needed 1- 0 swap do
        dup i rshift 1 and [char] 0 + emit
        i 4 mod 0= i 0> and if [char] _ emit then
    -1 +loop drop ;

: .all ( n -- )
    ." dec " dup 0 .r
    dup 0< if ."  (unsigned " dup 10 u.base ." )" then
    ."   hex 0x" dup 16 u.base
    ."   oct 0o" dup 8 u.base
    ."   bin " dup .bin
    dup 32 127 within if ."   char '" dup emit ." '" then
    drop ;

\ ---- the token interpreter ------------------------------------------
\ Forth already reads RPN, so most tokens are handed straight to it.
\ Number-looking tokens get two extras first: 0b / 0o prefixes, and _
\ as a digit separator (1_000_000, 0xdead_beef). Interpreter state lives
\ in variables so it never touches the calculator's data stack.

create tok-buf 128 allot
variable tok-len
variable line-addr  variable line-len
variable cur-addr   variable cur-len               \ token being run, for errors

: number-like? ( a u -- f )
    dup 0= if 2drop false exit then
    over c@ [char] 0 [char] 9 1+ within if 2drop true exit then
    1 > swap dup c@ [char] - = swap 1+ c@ [char] 0 [char] 9 1+ within and and ;

: strip-underscores ( a u -- a' u' )
    0 tok-len !
    0 ?do dup i + c@ dup [char] _ <> if
        tok-buf tok-len @ + c! 1 tok-len +!
      else drop then
    loop drop tok-buf tok-len @ ;

: char>digit ( c -- n )                         \ 99 for anything not a digit
    dup [char] 0 [char] 9 1+ within if [char] 0 - exit then
    dup [char] a [char] z 1+ within if [char] a - 10 + exit then
    dup [char] A [char] Z 1+ within if [char] A - 10 + exit then
    drop 99 ;

variable acc  variable radix
: parse-base ( a u base -- n )                  \ throws -13 on a bad digit
    radix ! 0 acc !
    bounds ?do
        i c@ char>digit dup radix @ >= if -13 throw then
        acc @ radix @ * + acc !
    loop acc @ ;

: prefixed? ( a u c -- f )                      \ "0<c>..." with digits after
    >r dup 2 > if over c@ [char] 0 = swap drop swap 1+ c@ r> = and
    else 2drop r> drop false then ;

: run-number ( a u -- n )
    2dup [char] b prefixed? if 2 /string 2 parse-base exit then
    2dup [char] o prefixed? if 2 /string 8 parse-base exit then
    evaluate ;

: run-token ( a u -- ... )
    2dup cur-len ! cur-addr !
    over c@ [char] - = over 1 > and
    if 1 /string strip-underscores
       2dup number-like? if run-number negate exit then
       \ not a negative number after all: put the '-' back
       tok-buf tok-buf 1+ tok-len @ move [char] - tok-buf c! tok-buf tok-len @ 1+
       evaluate exit then
    2dup number-like? if strip-underscores run-number else evaluate then ;

: next-token ( -- a u )                         \ u = 0 at end of line
    begin line-len @ 0> if line-addr @ c@ bl <= else false then
    while 1 line-addr +! -1 line-len +! repeat
    line-addr @ 0
    begin dup line-len @ < if over over + c@ bl > else false then
    while 1+ repeat
    dup negate line-len +! dup line-addr +! ;

: run-line ( -- ... )   begin next-token dup while run-token repeat 2drop ;

: error-text ( code -- a u )
    case
        -4  of s" stack underflow" endof
        -10 of s" division by zero" endof
        -13 of s" unknown word or bad number" endof
        -3  of s" stack overflow" endof
        >r s" error" r>
    endcase ;

variable failed
: evaluate-line ( a u -- ... )                  \ runs a line, reporting errors
    line-len ! line-addr ! false failed !
    ['] run-line catch ?dup if
        error-text type ." : " cur-addr @ cur-len @ type cr
        true failed !
    then ;
