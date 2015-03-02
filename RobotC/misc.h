#ifndef __MISC_H__
#define __MISC_H__

#define _T writeDebugStream

char dtxt[128];
float PI2 = 2.0 * PI;

float NormalizeAngle(float a)
{
	while (a > PI2)
		a -= PI2;
	while (a < -PI2)
		a += PI2;
	return a;
}

float NormalizeHeading(float h)
{
	while (h > PI2)
		h -= PI2;
	while (h < -PI2)
		h += PI2;
	return h;
}

float ToDegrees(float a)
{
	return a * 180.0 / PI;
}

float ToRadians(float a)
{
	return a * PI / 180.0;
}

float Distance(float x1, float y1, float x2, float y2)
{
	return sqrt((x2-x1)*(x2-x1) + (y2-y1)*(y2-y1));
}

#endif
