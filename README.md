
# H25-SIM-Eq6-IA-Reseau-Neurones

Par : Nicolas Gagné, Laurent Lepage, Jérôme Bruyère

Hiver 2025 - SIM - Bdeb - Projet d'intégration

À propos :

Ce projet consiste à la création d'une IA qui est capable d'apprendre par lui-même. Le projet-ci est le produit final qui permet d'experimenter avec les différents cerveaux pré-entrainés et aussi de manuellement contrôler l'alpiniste. 

Librairie utilisée: Unity ML-agents toolkit

2 éléments seron remis : 

1- le code sur github ici

2- le build du projet

Étapes pour faire fonctionner le projet dans l'editor:
1- Installer python 3.10.x

2- Installer le repo pour ML-Agents 

3- Cloner le repo du projet pour avoir l'emplacement en local et ouvrir le projet avec Unity

4- Dans l'emplacement du dossier du projet, ouvrir l'invite de commandes ("command prompt")

**étapes suivantes (5.x) obligatoires seulement si 1ère utilisation du projet**

5.0- ouvrir la version de python dans les commandes, écrire : py -3.10 -m venv venv

5.1- Dans l'invite de commandes écrire : venv\Scripts\activate

5.2- Ensuite, quand le venv est activé ( "(venv)" écrit au début de la ligne), écrire py -m pip install --upgrade pip

5.3- écrire : pip install mlagents

5.4- écrire : pip install torch torchvision torchaudio

5.5- pour vérifier si tout fonctionne écrire : mlagents-learn --help

**étape suivante (5) si pas 1ère utilisation**

5- faire étape 5.1 (et 5.5 pour vérifier)

6- pour entrainer écrire: mlagents-learn... vérifier la documentation de mlagents pour la suite


