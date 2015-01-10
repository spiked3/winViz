task main()
{
	char tmp[128];
	// signed unsigned tests
  ubyte a = 0x00;
  ubyte b = 0x00;

  sprintf(tmp, "a,b: %d, %d", a, b);
  writeDebugStreamLine(tmp);

  a = 0xff;
  sprintf(tmp, "a,b: %d, %d", a, b);
  writeDebugStreamLine(tmp);

  a = 0;
  b = 0xff;

  sprintf(tmp, "a,b: %d, %d", a, b);
  writeDebugStreamLine(tmp);

  int c = (int)a + ((int)b << 8);
  if (c > 128)
  	c = c - 256;
  sprintf(tmp, "a,b = c : %d, %d, %d", a, b, c);
  writeDebugStreamLine(tmp);

  a = 0xff;
  c = (int)a + ((int)b << 8);
  if (c > 128)
  	c = c - 256;
  sprintf(tmp, "a,b = c : %d, %d, %d", a, b, c);
  writeDebugStreamLine(tmp);

}
