using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlterableExemple : MonoBehaviour
{

    //Exemple :
    //Variable a mettre dans les composants qui doit être altéré
    [SerializeField] float baseSpeed = 3;
    public Alterable<float> CurrentSpeed { get; private set; }

    //ADD parameter exemple
    //3eme parametre = le poid (donc la priorité sur l'application de cet l'effet sur la variable)
    //var t1 = movement.CurrentSpeed.AddTransformator(f => f * 1.8f, 100);

    //Appel du calcul lors d'utilisation de la variable
    // rb.velocity = moveDir * CurrentSpeed.CalculValue();


    private void Awake()
    {
        CurrentSpeed = new Alterable<float>(baseSpeed);
    }

    public void RemoveNothing()
    {
        //Arrange
        Alterable<int> a = new Alterable<int>(0);
        var ticket = a.AddTransformator(i => i * 10, 10);

        //Act
        a.RemoveTransformator(null);
    }

    public void AlterableProcess()
    {
        //Arrange
        Alterable<int> a = new Alterable<int>(10);
        a.AddTransformator(i => i * 10, 10);

        //Act
        var result = a.CalculValue();
    }

    public void AlterableProcessWithTwoPasses()
    {
        //Arrange
        Alterable<int> a = new Alterable<int>(10);
        a.AddTransformator(i => i + 10, 20);
        a.AddTransformator(i => i * 10, 10);

        // Il fait d'abord la multiplication malgrès le fait que le transformator a été fait en 2eme car il un poid de 10.
        // Donc 10 * 10 = 10
        // Ensuite on lui donne l'addition à faire, donc il va faire 100 + 10 et non pas 100 + (10 + 10). On ne réutilise pas a chaque fois la valeur de départ dans notre calcul

        //Act
        var result = a.CalculValue();
    }
}
