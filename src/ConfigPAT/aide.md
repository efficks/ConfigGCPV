## Outil d'aide au montage de compétition

Cet outil permet de charger des inscriptions produites par le système IceReg en
format XLSX et de les intégrer dans une compétiton contenu dans un fichier PAT.
Pour bien fonctionner, vos fichier doivent répondrent à certains critères.

### Fichier PAT
1. La compétition de destination doit déjà exister dans le fichier PAT.
2. Les clubs de l'inscription doivent déjà exister dans le fichier PAT et l'accronyme doit correspondre aux inscriptions.

### Fichier d'inscription
Le fichier XLSX doit respecter certains critères.

1. Les entêtes doivent contenir la première ligne doit contenir les nom de colonne suivants : First Name, Last Name, Sex, DOB, Membership Numbers, Affiliates
2. La colonne Affiliates doit contenir l'abréviation du club entre parenthèse après le nom du club.
3. Les abréviations de club doivent être identiques aux abréviations du fichier PAT de destination.
4. La colonne DOB, date de naissance doit être en format Date : AAAA-MM-JJ
5. La colonne Sex, doit contenir Male pour un garçon et Female pour une fille

### Importation

Lancez l'importation en choisissant le fichier d'inscription et le fichier PAT de destination et appuyez sur le boutton "Lancer l'importation".

Lors de l'importation
* L'outil considère un patineur comme étant le même s'ils ont le même prénom, nom, sexe, date de naissance, club et numéro de membre.
* Si deux patineurs ont le même numéro de membre mais avec des informations différentes, l'outil vous demandera ce que vous voulez faire.
* Vous pourrez choisir de prendre les informations de l'inscription, du fichier PAT ou d'arrêter l'importation pour faire la correction manuellement.
* Si une inscription correspond à un patineur avec la même date de naissance, prénom et nom, elle sera fusionné dans le patineur et le numéro de membre sera corrigé dans le fichier PAT
* Une inscription qui ne correspond à aucun patineur dans le fichier PAT sera ajoutée dans le fichier automatiquement.
* L'outil vous demandera dans quelle compétition vous souhaitez ajouter les inscriptions.

L'outil d'importation fera aussi la correstion des catégories et va créer les catégories suivantes.
* 5-6 ans
* 7-8 ans
* 9-10 ans
* 11-14 ans

À la fin de l'importation tous les patineurs du fichier PAT auront la bonne catégorie.

VOUS DEVEZ VÉRIFIER manuellement à la fin s'il y a des patineurs en double et faire la correction manuellement.

## License
    <one line to give the program's name and a brief idea of what it does.>
    Copyright (C) <year>  <name of author>

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
