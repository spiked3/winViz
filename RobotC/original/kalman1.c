#pragma config(Sensor, S1,     AIMU,           sensorI2CCustom9V)
#pragma config(Sensor, S2,     COMPASS,        sensorI2CHiTechnicCompass)
//*!!Code automatically generated by 'ROBOTC' configuration wizard               !!*//

/*************************************************************************************************\
*																																																	*
* PROGRAM: Heading    	      																																		*
* VERSION: 0																																											*
* PURPOSE: This program integrates gyro and compass readings using a Kalman filter to get a more	*
*					 accurate value for heading                     																				*
* AUTHOR:	 Aswin Bouwmeester (aswin.bouwmeester@gmail.com)																				*
* MODIFIED:mike partain (http://www.spiked3.com)																				          *
* DATE:		 okt 10, 2010, aug 27, 2012																															*
*																																																	*
\*************************************************************************************************/

/*
variance for the gyro sensor for different sample sizes

N   T   var
1	  5	  0,222
2	  10	0,144
3	  15	0,123
4	  20	0,119
5	  25	0,114
6	  30	0,088
8	  40	0,093
10	50	0,078
*/

// seems to follow, if I move the compass slowly
// as soon as I move it quick, it reports as 'disturbed' and sometimes catches back up, sometimes not
// how did we get the variances? (i see how they are used, but where did they come from?)
// answer: http://bilgin.esme.org/BitsBytes/KalmanFilterforDummies.aspx
//		is on that web page although I'm still not 100% understanding it
// and obviously, why does it sometimes settle back to undisturbed, and sometimes not?

#include "drivers/MSIMU-driver.h"

#pragma autoStartTasks
#define HEADING_DISPLAY

// MOUNTING can be set to - if the compass or the gyro is mouned upside down
#ifndef MOUNTING
#define MOUNTING
#endif

float meanGyroZValue(int interval, int N);
float meanHeadingValue(int interval, int N);
float heading = -1.0;

task getheading()
{
	writeDebugStreamLine("*getheading");
	float var_compass  =  0.9,
	var_gyro = 0.119,
	var_filter_predicted,
	var_filter_updated,
	kalman_gain,
	compass_measured,
	compass_predicted,
	compass_updated,
	gyro_measured,
	time_span,
	offset;

	long time_start, time_end;

	bool disturbed;

	// get the gyro offset
	offset = meanGyroZValue(5,100);
	writeDebugStreamLine("gyr offs %f", offset);

	// initialise the filter;
	compass_updated = SensorValue[COMPASS];
	var_filter_updated = 0;

	// Run the filter forever;
	while (true)
	{
		// get time span;
		time_end = nPgmTime;
		time_span = ((float)(time_end - time_start)) / 1000.0;
		if (time_span <= 0) {
			time_span = 0.02; // this is to compensate for wrapping around the nPgmtime variable;
			writeDebugStreamLine("time span wrap");
		}
	  time_start = nPgmTime;

		// get measurements from sensors
		// (when changing the sample size of the gyro, one must also change the variance)
		compass_measured = (float)SensorValue[COMPASS];
		gyro_measured = MOUNTING ( meanGyroZValue(5,4) - offset);

		// predict;
		compass_predicted = compass_updated + time_span * gyro_measured;
		var_filter_predicted = var_filter_updated + var_gyro;

		// heading must be between 0 and 359
		if (compass_predicted < 0)
			compass_predicted += 360;
		if (compass_predicted >= 360)
			compass_predicted -= 360;

		// Detect _compass disturbance;
		if (abs(compass_predicted - compass_measured) > 2 * sqrt(var_filter_predicted))
			disturbed = true;
		else
			disturbed = false;

		// get Kalman gain;
		if (disturbed)
			kalman_gain = 0;
		else
			kalman_gain = var_filter_predicted / (var_filter_predicted + var_compass);

		// update;
		compass_updated = compass_predicted + kalman_gain * (compass_measured - compass_predicted);
		var_filter_updated = var_filter_predicted + kalman_gain * (var_compass - var_filter_predicted);

		// make result available gobally
		heading = compass_updated;

		// display informatin about filter

#ifdef HEADING_DISPLAY
		nxtDisplayTextLine(0,"Heading filter");
		nxtDisplayTextLine(1,"Heading  : %3.0f",compass_updated);
		nxtDisplayTextLine(2,"Compass  : %3.0f",compass_measured);
		nxtDisplayTextLine(3,"Variance : %6f",var_filter_updated);
		nxtDisplayTextLine(4,"Disturbed: %1d  ",disturbed);
#endif

		// wait for next iteration;
		wait1Msec(0);
	}
}

float meanGyroZValue(int interval, int N)
{
	float som = 0;
	for(int i = 0; i < N; i++)
	{
		som +=  (float)MSIMUreadGyroZAxis(AIMU);
		wait1Msec(interval);
	}
	som = som / N;
	return som;
}

float meanHeadingValue(int interval, int N)
{
	float som = 0;
	for(int i = 0; i < N; i++)
	{
		som += (float)SensorValue[COMPASS];
		wait1Msec(interval);
	}
	som = som / N;
	return som;
}

task main()
{
	writeDebugStreamLine("*main");
	while (true)
	{
		wait1Msec(1000);
	}
}