namespace SprutTechnology.SCPostprocessor{
    public class NCSub{
        public InpArray<string> Name;

        public NCSub(){
            Name = new InpArray<string>();
        }

        public void AddSub(string value, int i) => Name[i] = value;
    }
}