import { Component, Injector, OnInit, EventEmitter, Output, ChangeDetectorRef } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { KolServiceProxy, CreateKolDto, ChannelType, CareerServiceProxy, CareerDto } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { AbpValidationSummaryComponent } from '../../../shared/components/validation/abp-validation.summary.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { Select } from 'primeng/select';
import { MultiSelect } from 'primeng/multiselect';
import { InputNumber } from 'primeng/inputnumber';
import { Button } from 'primeng/button';

@Component({
    templateUrl: './create-kol-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        AbpModalHeaderComponent,
        AbpValidationSummaryComponent,
        LocalizePipe,
        InputText,
        Textarea,
        Select,
        MultiSelect,
        InputNumber,
        Button,
    ],
})
export class CreateKolDialogComponent extends AppComponentBase implements OnInit {
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    kol = new CreateKolDto();
    careers: CareerDto[] = [];

    channelOptions = [
        { value: ChannelType.Tiktok, label: 'TikTok' },
        { value: ChannelType.Facebook, label: 'Facebook' },
        { value: ChannelType.Khac, label: 'Khác' },
    ];

    constructor(
        injector: Injector,
        public _kolService: KolServiceProxy,
        private _careerService: CareerServiceProxy,
        public bsModalRef: BsModalRef,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.kol.channel = ChannelType.Tiktok;
        this.kol.follow = 0;
        this._careerService.getAll(undefined, 'Name', 0, 1000).subscribe((r) => {
            this.careers = r.items ?? [];
            this.cd.detectChanges();
        });
    }

    save(): void {
        this.saving = true;
        this._kolService.create(this.kol).subscribe({
            next: () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.bsModalRef.hide();
                this.onSave.emit();
            },
            error: () => { this.saving = false; },
        });
    }
}
