# VR Platform 3
# Intended to run with VR Server 3

This Unity project contains prefabs required in order to read data from the server, properly position the camera for the user perspective, and position any other objects associated with motion-captured objects.

To build a new project:

1.) Create a new Unity scene

2.) Drop in the MasterStream prefab. Ensure that the MasterStream script is attached to it.

3.) Drop in the PlayerController prefab. Ensure the PlayerController script is properly attached to it. You will need to change a few parameters: first, change the label to match whatever rigid body is being used to track the user headset in Motive. You will also need to associate the MasterStream object to this PlayerController. To do so, simply drag and drop the MasterStream object you have created into the PlayerController script's M Stream field. You may leave the offset unchanged.

4.) For any object that is tracked in Motive with a rigid body label 'x', create an empty object and add the ObjectController script. Similarly to the PlayerController, modify the 'label' property and associate the Master Stream object to the Object Controller script. Then, as a child of this object, create the object that you would like to move with the tracked object. This can be geometry, particles, or anything else.

5.) Add in a supporting environment and anything else you will need.

6.) Have fun!
