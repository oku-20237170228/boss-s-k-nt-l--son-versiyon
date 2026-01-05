using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CoinManager : MonoBehaviour
{
    public int CoinCount;
    public TMP_Text CoinText;


    public void Start()
    {
        
    }
    void Update()
    {
        CoinText.text = CoinCount.ToString();
        
    }

}
