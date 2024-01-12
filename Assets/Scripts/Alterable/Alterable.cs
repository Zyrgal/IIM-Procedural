using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class Alterable<T> //Template
{
    private class Transformateur
    {
        Func<T, T> _method;
        int _weight;
        object _label;

        public Transformateur(Func<T, T> method, int weight, object label)
        {
            _method = method;
            _weight = weight;
            _label = label;
        }

        public Func<T, T> Method { get => _method; set => _method = value; }
        public int Weight { get => _weight; set => _weight = value; }
        public object Label { get => _label; set => _label = value; }
    }

    private T startValue;
    List<Transformateur> _data;

    public Alterable(T initialValue)
    {
        startValue = initialValue;
        _data = new List<Transformateur>();
    }

    public T StartValue { get => startValue; set => startValue = value; }

    public object AddTransformator(Func<T, T> method, int weight)
    {

        //Guard NEW VERSION
        Assert.IsNotNull(method); //Avec Assert cette expression ne sera pas prise en compte dans la build, on gagne des perfs
        Assert.IsTrue(weight >= 0);

#if UNITY_EDITOR
        //Guard (version 2) OLD VERSION
        if (method == null) throw new ArgumentNullException(nameof(method));
        if (weight < 0) throw new ArgumentNullException(nameof(weight));
#endif
        var newTransformator = new Transformateur(method,weight,new object());

        int idx = 0;

        for (int i = 0; i < _data.Count; i++)
        {
            /*if (_data[idx].Weight < weight)
            {
                continue;
            }
            else
            {
                break;
            }*/

            //Mieux que au dessus
            if (_data[idx].Weight > weight) break;

        }

        _data.Insert(idx, newTransformator);

        //_data.Select((i, idx) => (i, idx)).First(i => i.i.Weight > weight).idx, newTransformator);

        return newTransformator.Label;
    }

    public void RemoveTransformator(object label)
    {
        _data.Remove(_data.First(i => i.Label == label));

        // Method qui marche aussi
        /*for (int i = 0; i < _data.Count; i++)
        {
            if (_data[i].Label == label)
            {
                _data.Remove(_data[i]);
                return;
            }
        }*/



        //Ne pas faire un foreach car sinon on va remove un object dans une liste qu'on parcours
        /*foreach (var item in collection)
        {

        }*/



    }

    /*public object AddTempTransformator(Func<T, T> method, int weight, float timeActive)
    {
        var obj = AddTransformator(method, weight);

        PlayerBrain.Instance.StartCoroutine(RemoveTempTransformator(method, timeActive));

        return obj;
    }

    IEnumerator RemoveTempTransformator(Func<T, T> method, float timeActive)
    {
        yield return new WaitForSeconds(timeActive);

        RemoveTransformator(method);
    }*/

    public T CalculValue()
    {
        T tmp = startValue;
        //float? tmp = 0f;

        foreach (var el in _data)
        {
            tmp = el.Method.Invoke(tmp);
            //tmp = el.Method?.Invoke(tmp) ?? 0f; //Si c'est null alors la valeur est égale à 0, mais bon le calcul sera faussé anyway
        }

        return tmp;
    }
}


