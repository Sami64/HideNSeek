using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerColor : NetworkBehaviour
{
    private readonly NetworkVariable<Color> _netColor = new();
    private readonly Color[] _colors = { Color.red, Color.blue, Color.green, Color.yellow, Color.black, Color.white, Color.magenta, Color.gray };
    private int _index;

    [SerializeField] MeshRenderer _renderer;
    
    // Start is called before the first frame update
    void Awake()
    {
        _netColor.OnValueChanged += OnColorValueChanged;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _netColor.OnValueChanged -= OnColorValueChanged;
    }

    void OnColorValueChanged(Color prev, Color next)
    {
        _renderer.material.color = next;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _index = (int)OwnerClientId;
            NetworkColorServerRpc(GetNextColor());
        }
        else
        {
            _renderer.material.color = _netColor.Value;
        }
    }

    [ServerRpc]
    void NetworkColorServerRpc(Color color)
    {
        _netColor.Value = color;
    }

    Color GetNextColor()
    {
        return _colors[_index++ % _colors.Length];
    }
}
