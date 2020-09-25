# -*- coding: utf-8 -*-

import json
from zmq import Context, DEALER
from .params import TextToSpeechParams

class TTSRequester(object):
    def __init__(self, context, ip, port, config_port):
        self._context = context
        self._socket = self._context.socket(DEALER)
        self._socket.identity = b"client"
        self._socket.connect('tcp://{}:{}'.format(ip, port))
        self._msg_id = 0
        self.params = TextToSpeechParams(context, ip, config_port)

    def say(self, sentence, blocking = True):
        #HACK : adding a space for each non ascii character
        for i in range(len(sentence)):
            if ord(sentence[i])<128:
                sentence+= ' '
        self._socket.send_json({"id": self._msg_id, "blocking": blocking, "sentence": sentence})
        self._msg_id += 1
        message = self._socket.recv()#_multipart()
        return None

    def start(self):
        """
        Take into account the parameters and start the TTS system
        Returns an empty string if the parameters have been set successfully on the server or an error string otherwise
        """
        return self.params.send_params()
