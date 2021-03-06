#pragma config(Sensor, S2,     AIMU,           sensorI2CCustom9V)
//#pragma config(Sensor, S4,     ,               sensorHighSpeed)
//*!!Code automatically generated by 'ROBOTC' configuration wizard               !!*//

/* xbee1 */
#include "drivers/MSIMU-driver.h"

#define _TL writeDebugStreamLine

TFileIOResult ior;

void dataLog(int idx, int t, float f)
{
	ior = nxtWriteRawHS(idx, 2);
	wait1Msec(1);

	ior = nxtWriteRawHS(t, 2);
	wait1Msec(1);

	ior = nxtWriteRawHS(f, 4);
	wait1Msec(1);
}

task main()
{
	clearDebugStream();
	writeDebugStreamLine("xbee5");

	time1[T1] = 0;

	nxtEnableHSPort();            //Enable High Speed Port #4
	nxtSetHSBaudRate(115200);  			//Xbee Default Speed
	nxtHS_Mode = hsRawMode;       //Set to Raw Mode (vs. Master/Slave Mode)

	while (true)
	{
		int t = time1[T1];
		int ax, ay, az, tx, ty, tz, gx, gy, gz;

		MSIMUreadAccelAxes(AIMU, ax, ay, az);
		MSIMUreadTiltAxes(AIMU, tx, ty, tz);
		MSIMUreadGyroAxes(AIMU, gx, gy, gz);

		dataLog(7, t, (az / 100.0));
		dataLog(8, t, (tz / 1000.0));
		dataLog(9, t, gz);

		wait1Msec(50);
	}

}
