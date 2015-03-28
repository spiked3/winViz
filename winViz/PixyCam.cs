using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spiked3.winViz
{
    class PixyCam
    {
    }
}

#if false
//
// begin license header
//
// This file is part of Pixy CMUcam5 or "Pixy" for short
//
// All Pixy source code is provided under the terms of the
// GNU General Public License v2 (http://www.gnu.org/licenses/gpl-2.0.html).
// Those wishing to use Pixy source code, software and/or
// technologies under different licensing terms should contact us at
// cmucam@cs.cmu.edu. Such licensing terms are available for
// all portions of the Pixy codebase presented here.
//
// end license header
//

..include <SPI.h>  
..include <Pixy.h>
..include <Servo.h>


int speed=1650;
..define STEERING 11		// Steering Servo is Digital Pin 11 on Arduino Mega 2560
..define NOMINAL_SPEED 1650      
..define HB25 10	// Parallax HB-25 motor controller is connected to Digital pin 10

Servo myservo;  // create servo object to control a servo 


Pixy pixy;

void setup()
{
  Serial.begin(9600);
  Serial.print("Starting...\n");
  myservo.attach(STEERING);
  speed = NOMINAL_SPEED;  
   Init_servo(); //Initalize the servos
}


 
void pulse_servo_throttle(void) 	
{
  pulseOut(HB25, speed);  //pulseWidth of 1500 is stop, 1000 is full reverse, 2000 is full forward
  delay(6);
  pulseOut(HB25, speed);  //pulseWidth of 1500 is stop, 1000 is full reverse, 2000 is full forward
  Serial.print("speed is ");
  Serial.println(speed);
}


void pulseOut(int pinNumber, int pulseWidth) {
 // only pulse if  pulseWidth > 0
 
  if (pulseWidth > 0) 
     {  
      digitalWrite(pinNumber, HIGH);
      delayMicroseconds(pulseWidth);
      digitalWrite(pinNumber, LOW);
      delayMicroseconds(pulseWidth + 5250);  //The HB-25 requires a delay of at least pulseWidth + 5250uS 
    }   
  }



void Init_servo(void)
{   
  digitalWrite(STEERING,LOW);//Defining servo output pins
  pinMode(STEERING,OUTPUT);
  digitalWrite(HB25,LOW);
  pinMode(HB25,OUTPUT);
}


void pulse_servo_steering(long angle) 
{
  Serial.print("setting steering to ");
  Serial.println(angle);

  if (angle>180)
   angle = 180;
  else if (angle<4)  // using 4 degrees for min angle instead of 0 deg to prevent servo chatter
   angle = 4;
   
  myservo.write(angle);              

}


void loop()
{ 
  static int i = 0;
  int j;
  uint16_t blocks;
  char buf[32]; 
  int32_t panError, tiltError;

  blocks = pixy.getBlocks();
  pulse_servo_throttle();
  
  if (blocks)
  {

    Serial.print("x position is --------->");
    Serial.println(pixy.blocks[0].x);
    Serial.print("servo position is --------->");
    Serial.println(pixy.blocks[0].x/1.76);
    pulse_servo_steering(180-pixy.blocks[0].x/1.76); // Pixy range is 0 to 318, servo range is 0 to 180, so divide by 318/180 = 1.76
    
    pulse_servo_throttle();   //HB-25 must be refreshed or the drive motor will stop
   
    i++;

    if (i%50==0)
    {
      sprintf(buf, "Detected %d:\n", blocks);
      Serial.print(buf);
      for (j=0; j<blocks; j++)
      {
        sprintf(buf, "  block %d: ", j);
        Serial.print(buf); 
        pixy.blocks[j].print();
      }
    }
  }  
}

#endif