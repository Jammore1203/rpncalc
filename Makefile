.PHONY: test install

test:
	gforth tests/test.fs
	./bin/rpncalc 0b1010_0101 0x0f and 4 shl > /tmp/rpncalc-out.txt
	printf '1 2 3\n+ *\n' | ./bin/rpncalc 2>/dev/null >> /tmp/rpncalc-out.txt
	diff -u tests/expected.txt /tmp/rpncalc-out.txt
	! ./bin/rpncalc 1 0 / > /dev/null
	@echo "CLI tests passed."

install:
	ln -sf "$(CURDIR)/bin/rpncalc" $(or $(PREFIX),/usr/local)/bin/rpncalc
